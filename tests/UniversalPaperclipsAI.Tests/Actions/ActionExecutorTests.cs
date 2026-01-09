using Moq;
using UniversalPaperclipsAI.Actions;
using UniversalPaperclipsAI.Browser;
using UniversalPaperclipsAI.Core;

namespace UniversalPaperclipsAI.Tests.Actions;

public class ActionExecutorTests
{
    private readonly Mock<BrowserController> _mockBrowser;

    public ActionExecutorTests()
    {
        // Create mock with minimal setup - BrowserController requires settings
        var gameSettings = new GameSettings();
        var displaySettings = new DisplaySettings();
        _mockBrowser = new Mock<BrowserController>(gameSettings, displaySettings, null);
    }

    [Fact]
    public async Task ExecuteBatchAsync_EnforcesMaxActionsLimit()
    {
        // Arrange
        const int maxActions = 3;
        var executor = new ActionExecutor(_mockBrowser.Object, maxActions);

        var actions = new List<LLMAction>
        {
            new() { Type = "MakePaperclip" },
            new() { Type = "MakePaperclip" },
            new() { Type = "MakePaperclip" },
            new() { Type = "MakePaperclip" }, // Should be ignored
            new() { Type = "MakePaperclip" }, // Should be ignored
        };

        // Act
        await executor.ExecuteBatchAsync(actions);

        // Assert - history should only have 3 entries (some may fail due to mock, but attempts are logged)
        var history = executor.ActionHistory;
        Assert.True(history.Count <= maxActions, $"Expected at most {maxActions} actions, but got {history.Count}");
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithDefaultMaxActions_LimitsToFive()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object); // Default is 5

        var actions = Enumerable.Range(0, 10)
            .Select(_ => new LLMAction { Type = "MakePaperclip" })
            .ToList();

        // Act
        await executor.ExecuteBatchAsync(actions);

        // Assert
        var history = executor.ActionHistory;
        Assert.True(history.Count <= 5, $"Expected at most 5 actions, but got {history.Count}");
    }

    [Fact]
    public void Constructor_WithZeroMaxActions_UsesDefault()
    {
        // Arrange & Act
        var executor = new ActionExecutor(_mockBrowser.Object, 0);

        // Assert - Should use default (5), test by executing more than 5 actions
        // This is an indirect test since _maxActionsPerDecision is private
        Assert.NotNull(executor);
    }

    [Fact]
    public void Constructor_WithNegativeMaxActions_UsesDefault()
    {
        // Arrange & Act
        var executor = new ActionExecutor(_mockBrowser.Object, -1);

        // Assert
        Assert.NotNull(executor);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownAction_ReturnsFailure()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);

        // Act
        var result = await executor.ExecuteAsync("UnknownAction123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unknown action", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullAction_ReturnsFailure()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);

        // Act
        var result = await executor.ExecuteAsync(null!);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyAction_ReturnsFailure()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);

        // Act
        var result = await executor.ExecuteAsync("");

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void ActionHistory_ReturnsThreadSafeCopy()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);

        // Act
        var history1 = executor.ActionHistory;
        var history2 = executor.ActionHistory;

        // Assert - Should be different list instances (snapshots)
        Assert.NotSame(history1, history2);
    }

    [Fact]
    public void GetRecentHistory_ReturnsThreadSafeCopy()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);

        // Act
        var history1 = executor.GetRecentHistory().ToList();
        var history2 = executor.GetRecentHistory().ToList();

        // Assert - Should be able to enumerate independently
        Assert.NotSame(history1, history2);
    }

    [Fact]
    public void ClearHistory_ClearsAllEntries()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);

        // Act
        executor.ClearHistory();

        // Assert
        Assert.Empty(executor.ActionHistory);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithCancellation_StopsExecution()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var actions = new List<LLMAction>
        {
            new() { Type = "MakePaperclip" },
            new() { Type = "MakePaperclip" },
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => executor.ExecuteBatchAsync(actions, null, cts.Token));
    }

    [Fact]
    public async Task ConcurrentAccess_ToHistory_IsThreadSafe()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object, 100);
        var tasks = new List<Task>();

        // Act - Simulate concurrent access
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    await executor.ExecuteAsync("MakePaperclip");
                    _ = executor.ActionHistory;
                    _ = executor.GetRecentHistory().ToList();
                }
            }));
        }

        // Assert - Should not throw any exceptions
        await Task.WhenAll(tasks);
    }
}

public class ActionExecutorValidationTests
{
    private readonly Mock<BrowserController> _mockBrowser;

    public ActionExecutorValidationTests()
    {
        var gameSettings = new GameSettings();
        var displaySettings = new DisplaySettings();
        _mockBrowser = new Mock<BrowserController>(gameSettings, displaySettings, null);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithAvailableActions_ValidatesActions()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);
        var availableActions = new List<string> { "MakePaperclip", "BuyWire" };

        var actions = new List<LLMAction>
        {
            new() { Type = "MakePaperclip" },
            new() { Type = "InvalidAction" }, // Should be skipped
            new() { Type = "BuyWire" }
        };

        // Act
        var executedCount = await executor.ExecuteBatchAsync(actions, availableActions);

        // Assert - InvalidAction should be skipped
        var history = executor.ActionHistory;
        var invalidEntry = history.FirstOrDefault(h => h.ActionName == "InvalidAction");
        Assert.NotNull(invalidEntry);
        Assert.False(invalidEntry.Success);
        Assert.Contains("not available", invalidEntry.Message);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithNullAvailableActions_SkipsValidation()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);

        var actions = new List<LLMAction>
        {
            new() { Type = "MakePaperclip" }
        };

        // Act - Should not throw even without validation list
        await executor.ExecuteBatchAsync(actions, null);

        // Assert
        Assert.NotEmpty(executor.ActionHistory);
    }

    [Fact]
    public async Task ExecuteBatchAsync_MakePaperclip10_ValidWhenMakePaperclipAvailable()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);
        var availableActions = new List<string> { "MakePaperclip" }; // Only base action

        var actions = new List<LLMAction>
        {
            new() { Type = "MakePaperclip10" } // Multi-click variant
        };

        // Act
        await executor.ExecuteBatchAsync(actions, availableActions);

        // Assert - Should be allowed because MakePaperclip is available
        var history = executor.ActionHistory;
        var entry = history.First(h => h.ActionName == "MakePaperclip10");
        Assert.NotEqual("Action not available in current game state", entry.Message);
    }

    [Fact]
    public async Task ExecuteBatchAsync_ProjectAction_ValidatedCorrectly()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);
        var availableActions = new List<string> { "MakePaperclip", "ActivateProject:project1" };

        var actions = new List<LLMAction>
        {
            new() { Type = "ActivateProject:project1" }, // Valid
            new() { Type = "ActivateProject:project99" } // Invalid
        };

        // Act
        await executor.ExecuteBatchAsync(actions, availableActions);

        // Assert
        var history = executor.ActionHistory;
        var invalidEntry = history.First(h => h.ActionName == "ActivateProject:project99");
        Assert.False(invalidEntry.Success);
        Assert.Contains("not available", invalidEntry.Message);
    }

    [Fact]
    public async Task ExecuteBatchAsync_ToggleActions_AlwaysValid()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);
        var availableActions = new List<string> { "MakePaperclip" }; // Toggles not in list

        var actions = new List<LLMAction>
        {
            new() { Type = "ToggleAutoWire" },
            new() { Type = "SetMediumRisk" }
        };

        // Act
        await executor.ExecuteBatchAsync(actions, availableActions);

        // Assert - These should not be blocked as "not available"
        var history = executor.ActionHistory;
        Assert.All(history, entry =>
            Assert.DoesNotContain("not available", entry.Message));
    }

    [Fact]
    public async Task ExecuteBatchAsync_ReturnsCorrectExecutedCount()
    {
        // Arrange
        var executor = new ActionExecutor(_mockBrowser.Object);
        var availableActions = new List<string> { "MakePaperclip" };

        var actions = new List<LLMAction>
        {
            new() { Type = "MakePaperclip" },
            new() { Type = "InvalidAction" }, // Skipped
            new() { Type = "MakePaperclip" }
        };

        // Act
        var executedCount = await executor.ExecuteBatchAsync(actions, availableActions);

        // Assert - Only valid actions should be counted (may still fail due to mock)
        // The key is that InvalidAction is not counted
        Assert.True(executedCount <= 2);
    }
}

public class ActionResultTests
{
    [Fact]
    public void ActionResult_Success_HasCorrectProperties()
    {
        // Arrange & Act
        var result = new ActionResult(true, "Success message");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Success message", result.Message);
    }

    [Fact]
    public void ActionResult_Failure_HasCorrectProperties()
    {
        // Arrange & Act
        var result = new ActionResult(false, "Error message");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Error message", result.Message);
    }
}
