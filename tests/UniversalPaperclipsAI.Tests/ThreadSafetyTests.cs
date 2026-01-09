using Moq;
using UniversalPaperclipsAI.Actions;
using UniversalPaperclipsAI.AI;
using UniversalPaperclipsAI.Browser;
using UniversalPaperclipsAI.Core;

namespace UniversalPaperclipsAI.Tests;

/// <summary>
/// Stress tests for thread safety of history collections.
/// These tests verify that concurrent access doesn't cause exceptions.
/// </summary>
public class ThreadSafetyTests
{
    [Fact]
    public async Task ActionExecutor_ConcurrentHistoryAccess_DoesNotThrow()
    {
        // Arrange
        var gameSettings = new GameSettings();
        var displaySettings = new DisplaySettings();
        var mockBrowser = new Mock<BrowserController>(gameSettings, displaySettings, null);
        var executor = new ActionExecutor(mockBrowser.Object, 1000);

        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Act - Simulate heavy concurrent access
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    for (int j = 0; j < 50; j++)
                    {
                        // Write operations
                        await executor.ExecuteAsync("MakePaperclip");

                        // Read operations
                        _ = executor.ActionHistory;
                        _ = executor.GetRecentHistory(5).ToList();
                        _ = executor.GetRecentHistory(10).ToList();
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task ActionExecutor_ConcurrentClearAndAccess_DoesNotThrow()
    {
        // Arrange
        var gameSettings = new GameSettings();
        var displaySettings = new DisplaySettings();
        var mockBrowser = new Mock<BrowserController>(gameSettings, displaySettings, null);
        var executor = new ActionExecutor(mockBrowser.Object, 100);

        var exceptions = new List<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var tasks = new List<Task>();

        // Act - Writers, readers, and clearers running concurrently
        // Writer task
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await executor.ExecuteAsync("MakePaperclip");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        }, cts.Token));

        // Reader task
        tasks.Add(Task.Run(() =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    _ = executor.ActionHistory;
                    _ = executor.GetRecentHistory().ToList();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        }, cts.Token));

        // Clearer task
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(100, cts.Token);
                    executor.ClearHistory();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        }, cts.Token));

        // Wait for tasks to complete or timeout
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation token fires
        }

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task DecisionEngine_ConcurrentHistoryAccess_DoesNotThrow()
    {
        // Arrange
        var settings = new OpenAISettings
        {
            ApiKey = "test-key-for-thread-safety-test",
            Model = "gpt-4-turbo"
        };
        var engine = new DecisionEngine(settings);

        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Act - Multiple readers accessing history concurrently
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 100; j++)
                    {
                        _ = engine.DecisionHistory;
                        _ = engine.LastDecision;
                        _ = engine.GetRecentDecisions(5).ToList();
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task ActionExecutor_HistoryPruning_MaintainsMaxSize()
    {
        // Arrange
        var gameSettings = new GameSettings();
        var displaySettings = new DisplaySettings();
        var mockBrowser = new Mock<BrowserController>(gameSettings, displaySettings, null);
        var executor = new ActionExecutor(mockBrowser.Object, 1000); // High limit per decision

        // Act - Execute many actions (more than MaxHistorySize of 500)
        for (int i = 0; i < 600; i++)
        {
            await executor.ExecuteAsync("MakePaperclip");
        }

        // Assert - History should be pruned to 500
        Assert.True(executor.ActionHistory.Count <= 500,
            $"History should be pruned to max 500, but was {executor.ActionHistory.Count}");
    }
}
