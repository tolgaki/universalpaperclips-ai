using UniversalPaperclipsAI.Actions;
using UniversalPaperclipsAI.AI;
using UniversalPaperclipsAI.Core;
using UniversalPaperclipsAI.GameState;
using UniversalPaperclipsAI.GameState.Models;

namespace UniversalPaperclipsAI.Tests.AI;

public class DecisionEngineTests
{
    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DecisionEngine(null!));
    }

    [Fact]
    public void Constructor_WithoutApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new OpenAISettings
        {
            ApiKey = "",
            Model = "gpt-4-turbo"
        };

        // Clear env var if set
        var originalEnvVar = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

        try
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new DecisionEngine(settings));
        }
        finally
        {
            // Restore env var
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalEnvVar);
        }
    }

    [Fact]
    public void DecisionHistory_ReturnsEmptyListInitially()
    {
        // Arrange
        var settings = CreateTestSettings();
        var engine = new DecisionEngine(settings);

        // Act
        var history = engine.DecisionHistory;

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public void DecisionHistory_ReturnsThreadSafeCopy()
    {
        // Arrange
        var settings = CreateTestSettings();
        var engine = new DecisionEngine(settings);

        // Act
        var history1 = engine.DecisionHistory;
        var history2 = engine.DecisionHistory;

        // Assert - Should be different instances (snapshots)
        Assert.NotSame(history1, history2);
    }

    [Fact]
    public void LastDecision_ReturnsNullInitially()
    {
        // Arrange
        var settings = CreateTestSettings();
        var engine = new DecisionEngine(settings);

        // Act
        var lastDecision = engine.LastDecision;

        // Assert
        Assert.Null(lastDecision);
    }

    [Fact]
    public void GetRecentDecisions_ReturnsEmptyInitially()
    {
        // Arrange
        var settings = CreateTestSettings();
        var engine = new DecisionEngine(settings);

        // Act
        var recent = engine.GetRecentDecisions();

        // Assert
        Assert.Empty(recent);
    }

    [Fact]
    public void GetRecentDecisions_ReturnsThreadSafeCopy()
    {
        // Arrange
        var settings = CreateTestSettings();
        var engine = new DecisionEngine(settings);

        // Act
        var recent1 = engine.GetRecentDecisions().ToList();
        var recent2 = engine.GetRecentDecisions().ToList();

        // Assert
        Assert.NotSame(recent1, recent2);
    }

    private static OpenAISettings CreateTestSettings()
    {
        return new OpenAISettings
        {
            ApiKey = "test-api-key-for-unit-tests",
            Model = "gpt-4-turbo",
            MaxTokens = 2000
        };
    }
}

public class LLMDecisionTests
{
    [Fact]
    public void LLMDecision_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var decision = new LLMDecision();

        // Assert
        Assert.Equal(string.Empty, decision.Reasoning);
        Assert.NotNull(decision.Actions);
        Assert.Empty(decision.Actions);
        Assert.Equal(string.Empty, decision.Priority);
    }

    [Fact]
    public void LLMDecision_CanSetProperties()
    {
        // Arrange
        var decision = new LLMDecision
        {
            Reasoning = "Test reasoning",
            Priority = "Test priority",
            Actions = new List<LLMAction>
            {
                new() { Type = "MakePaperclip" }
            }
        };

        // Assert
        Assert.Equal("Test reasoning", decision.Reasoning);
        Assert.Equal("Test priority", decision.Priority);
        Assert.Single(decision.Actions);
    }
}

public class DecisionLogEntryTests
{
    [Fact]
    public void DecisionLogEntry_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var entry = new DecisionLogEntry();

        // Assert
        Assert.Equal(default, entry.Timestamp);
        Assert.Equal(default, entry.GamePhase);
        Assert.Equal(string.Empty, entry.StateSnapshot);
        Assert.Equal(string.Empty, entry.Reasoning);
        Assert.NotNull(entry.Actions);
        Assert.Equal(string.Empty, entry.Priority);
        Assert.Equal(string.Empty, entry.RawResponse);
    }

    [Fact]
    public void DecisionLogEntry_CanSetAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var entry = new DecisionLogEntry
        {
            Timestamp = timestamp,
            GamePhase = GamePhase.Middle,
            StateSnapshot = "Test snapshot",
            Reasoning = "Test reasoning",
            Actions = new List<LLMAction> { new() { Type = "BuyWire" } },
            Priority = "Test priority",
            RawResponse = "Raw JSON response"
        };

        // Assert
        Assert.Equal(timestamp, entry.Timestamp);
        Assert.Equal(GamePhase.Middle, entry.GamePhase);
        Assert.Equal("Test snapshot", entry.StateSnapshot);
        Assert.Equal("Test reasoning", entry.Reasoning);
        Assert.Single(entry.Actions);
        Assert.Equal("Test priority", entry.Priority);
        Assert.Equal("Raw JSON response", entry.RawResponse);
    }
}
