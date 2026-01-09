using UniversalPaperclipsAI.GameState;
using UniversalPaperclipsAI.GameState.Models;

namespace UniversalPaperclipsAI.Tests.GameState;

public class EventDetectorTests
{
    private static GameStateSnapshot CreateBasicState(
        GamePhase phase = GamePhase.Early,
        long paperclips = 100,
        double funds = 10.0,
        double wire = 500,
        int trust = 2)
    {
        return new GameStateSnapshot
        {
            Phase = phase,
            Resources = new Resources
            {
                Paperclips = paperclips,
                Funds = funds,
                Wire = wire,
                Trust = trust
            },
            Manufacturing = new Manufacturing(),
            Business = new Business(),
            Computing = new Computing(),
            Investments = new Investments(),
            Space = new SpacePhase(),
            Projects = new ProjectList(),
            AvailableActions = new List<string> { "MakePaperclip", "BuyWire" }
        };
    }

    [Fact]
    public void ShouldTriggerDecision_OnFirstState_ReturnsTrue()
    {
        // Arrange
        var detector = new EventDetector();
        var state = CreateBasicState();

        // Act
        var shouldTrigger = detector.ShouldTriggerDecision(state);

        // Assert
        Assert.True(shouldTrigger);
    }

    [Fact]
    public void ShouldTriggerDecision_OnPhaseChange_ReturnsTrue()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 60000); // Long interval
        var state1 = CreateBasicState(phase: GamePhase.Early);
        var state2 = CreateBasicState(phase: GamePhase.Middle);

        // Act
        detector.ShouldTriggerDecision(state1); // First state
        var shouldTrigger = detector.ShouldTriggerDecision(state2);

        // Assert
        Assert.True(shouldTrigger);
    }

    [Fact]
    public void ShouldTriggerDecision_OnTrustChange_ReturnsTrue()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 60000);
        var state1 = CreateBasicState(trust: 2);
        var state2 = CreateBasicState(trust: 3);

        // Act
        detector.ShouldTriggerDecision(state1);
        var shouldTrigger = detector.ShouldTriggerDecision(state2);

        // Assert
        Assert.True(shouldTrigger);
    }

    [Fact]
    public void ShouldTriggerDecision_OnWireLow_ReturnsTrue()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 60000);
        var state1 = CreateBasicState(wire: 150);
        var state2 = CreateBasicState(wire: 50); // Dropped below 100

        // Act
        detector.ShouldTriggerDecision(state1);
        var shouldTrigger = detector.ShouldTriggerDecision(state2);

        // Assert
        Assert.True(shouldTrigger);
    }

    [Fact]
    public void ShouldTriggerDecision_OnFundsThresholdCrossed_ReturnsTrue()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 60000);
        var state1 = CreateBasicState(funds: 90);
        var state2 = CreateBasicState(funds: 110); // Crossed 100

        // Act
        detector.ShouldTriggerDecision(state1);
        var shouldTrigger = detector.ShouldTriggerDecision(state2);

        // Assert
        Assert.True(shouldTrigger);
    }

    [Fact]
    public void ShouldTriggerDecision_OnPaperclipsThresholdCrossed_ReturnsTrue()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 60000);
        var state1 = CreateBasicState(paperclips: 900);
        var state2 = CreateBasicState(paperclips: 1100); // Crossed 1000

        // Act
        detector.ShouldTriggerDecision(state1);
        var shouldTrigger = detector.ShouldTriggerDecision(state2);

        // Assert
        Assert.True(shouldTrigger);
    }

    [Fact]
    public void ShouldTriggerDecision_OnNewProjectAvailable_ReturnsTrue()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 60000);
        var state1 = CreateBasicState();
        state1.Projects.Available.Add(new Project { Id = "project1", Title = "Test Project 1" });

        var state2 = CreateBasicState();
        state2.Projects.Available.Add(new Project { Id = "project1", Title = "Test Project 1" });
        state2.Projects.Available.Add(new Project { Id = "project2", Title = "Test Project 2" }); // New

        // Act
        detector.ShouldTriggerDecision(state1);
        var shouldTrigger = detector.ShouldTriggerDecision(state2);

        // Assert
        Assert.True(shouldTrigger);
    }

    [Fact]
    public void ShouldTriggerDecision_OnNewActionAvailable_ReturnsTrue()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 60000);
        var state1 = CreateBasicState();
        state1.AvailableActions = new List<string> { "MakePaperclip" };

        var state2 = CreateBasicState();
        state2.AvailableActions = new List<string> { "MakePaperclip", "BuyAutoClipper" }; // New action

        // Act
        detector.ShouldTriggerDecision(state1);
        var shouldTrigger = detector.ShouldTriggerDecision(state2);

        // Assert
        Assert.True(shouldTrigger);
    }

    [Fact]
    public void ShouldTriggerDecision_OnScheduledInterval_ReturnsTrue()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 0); // Immediate interval
        var state1 = CreateBasicState();
        var state2 = CreateBasicState(); // Same state, but interval elapsed

        // Act
        detector.ShouldTriggerDecision(state1);
        var shouldTrigger = detector.ShouldTriggerDecision(state2);

        // Assert
        Assert.True(shouldTrigger);
    }

    [Fact]
    public void ShouldTriggerDecision_NoSignificantChange_ReturnsFalse()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 60000); // Long interval
        var state1 = CreateBasicState(paperclips: 100, funds: 50, wire: 500);
        var state2 = CreateBasicState(paperclips: 110, funds: 55, wire: 490); // Minor changes

        // Act
        detector.ShouldTriggerDecision(state1);
        var shouldTrigger = detector.ShouldTriggerDecision(state2);

        // Assert
        Assert.False(shouldTrigger);
    }

    [Fact]
    public void GetTriggerReasons_OnFirstState_ReturnsInitialState()
    {
        // Arrange
        var detector = new EventDetector();
        var state = CreateBasicState();

        // Act
        var reasons = detector.GetTriggerReasons(state);

        // Assert
        Assert.Contains("Initial state", reasons);
    }

    [Fact]
    public void GetTriggerReasons_OnPhaseChange_ContainsPhaseChange()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 60000);
        var state1 = CreateBasicState(phase: GamePhase.Early);
        var state2 = CreateBasicState(phase: GamePhase.Middle);

        // Act
        detector.ShouldTriggerDecision(state1);
        var reasons = detector.GetTriggerReasons(state2);

        // Assert
        Assert.Contains(reasons, r => r.Contains("Phase changed"));
    }

    [Fact]
    public void GetTriggerReasons_OnNewProject_ContainsNewProject()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 60000);
        var state1 = CreateBasicState();
        var state2 = CreateBasicState();
        state2.Projects.Available.Add(new Project { Id = "project1" });

        // Act
        detector.ShouldTriggerDecision(state1);
        var reasons = detector.GetTriggerReasons(state2);

        // Assert
        Assert.Contains("New project available", reasons);
    }

    [Fact]
    public void GetTriggerReasons_MultipleReasons_ReturnsAll()
    {
        // Arrange
        var detector = new EventDetector(decisionIntervalMs: 0); // Immediate interval
        var state1 = CreateBasicState(phase: GamePhase.Early, trust: 2);
        var state2 = CreateBasicState(phase: GamePhase.Middle, trust: 3);

        // Act
        detector.ShouldTriggerDecision(state1);
        var reasons = detector.GetTriggerReasons(state2);

        // Assert
        Assert.True(reasons.Count >= 2, "Expected multiple trigger reasons");
    }
}
