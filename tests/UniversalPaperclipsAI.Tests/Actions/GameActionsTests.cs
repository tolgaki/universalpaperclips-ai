using UniversalPaperclipsAI.Actions;

namespace UniversalPaperclipsAI.Tests.Actions;

public class GameActionsTests
{
    [Fact]
    public void All_ContainsExpectedManufacturingActions()
    {
        // Assert
        Assert.True(GameActions.All.ContainsKey("MakePaperclip"));
        Assert.True(GameActions.All.ContainsKey("MakePaperclip10"));
        Assert.True(GameActions.All.ContainsKey("BuyWire"));
        Assert.True(GameActions.All.ContainsKey("BuyAutoClipper"));
        Assert.True(GameActions.All.ContainsKey("BuyMegaClipper"));
    }

    [Fact]
    public void All_ContainsExpectedBusinessActions()
    {
        // Assert
        Assert.True(GameActions.All.ContainsKey("RaisePrice"));
        Assert.True(GameActions.All.ContainsKey("LowerPrice"));
        Assert.True(GameActions.All.ContainsKey("BuyMarketing"));
    }

    [Fact]
    public void All_ContainsExpectedComputingActions()
    {
        // Assert
        Assert.True(GameActions.All.ContainsKey("BuyProcessor"));
        Assert.True(GameActions.All.ContainsKey("BuyMemory"));
        Assert.True(GameActions.All.ContainsKey("QuantumCompute"));
    }

    [Fact]
    public void All_ContainsExpectedInvestmentActions()
    {
        // Assert
        Assert.True(GameActions.All.ContainsKey("Deposit"));
        Assert.True(GameActions.All.ContainsKey("Withdraw"));
        Assert.True(GameActions.All.ContainsKey("SetLowRisk"));
        Assert.True(GameActions.All.ContainsKey("SetMediumRisk"));
        Assert.True(GameActions.All.ContainsKey("SetHighRisk"));
    }

    [Fact]
    public void All_ContainsExpectedSpaceActions()
    {
        // Assert
        Assert.True(GameActions.All.ContainsKey("LaunchProbe"));
        Assert.True(GameActions.All.ContainsKey("MakeFactory"));
        Assert.True(GameActions.All.ContainsKey("MakeHarvester"));
        Assert.True(GameActions.All.ContainsKey("MakeWireDrone"));
    }

    [Fact]
    public void Get_WithValidAction_ReturnsDefinition()
    {
        // Act
        var action = GameActions.Get("MakePaperclip");

        // Assert
        Assert.NotNull(action);
        Assert.Equal("MakePaperclip", action.Name);
        Assert.Equal("#btnMakePaperclip", action.Selector);
    }

    [Fact]
    public void Get_WithInvalidAction_ReturnsNull()
    {
        // Act
        var action = GameActions.Get("NonExistentAction");

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Get_WithNullAction_ReturnsNull()
    {
        // Act
        var action = GameActions.Get(null!);

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void GetActionNames_ReturnsAllActionNames()
    {
        // Act
        var names = GameActions.GetActionNames().ToList();

        // Assert
        Assert.NotEmpty(names);
        Assert.Contains("MakePaperclip", names);
        Assert.Contains("BuyWire", names);
        Assert.Contains("LaunchProbe", names);
    }

    [Fact]
    public void MakePaperclip10_HasClickCountOf10()
    {
        // Act
        var action = GameActions.Get("MakePaperclip10");

        // Assert
        Assert.NotNull(action);
        Assert.Equal(10, action.ClickCount);
    }

    [Fact]
    public void SliderActions_HaveCorrectSliderValues()
    {
        // Act
        var lowRisk = GameActions.Get("SetLowRisk");
        var mediumRisk = GameActions.Get("SetMediumRisk");
        var highRisk = GameActions.Get("SetHighRisk");

        // Assert
        Assert.NotNull(lowRisk);
        Assert.True(lowRisk.IsSlider);
        Assert.Equal(2, lowRisk.SliderValue);

        Assert.NotNull(mediumRisk);
        Assert.True(mediumRisk.IsSlider);
        Assert.Equal(5, mediumRisk.SliderValue);

        Assert.NotNull(highRisk);
        Assert.True(highRisk.IsSlider);
        Assert.Equal(8, highRisk.SliderValue);
    }

    [Fact]
    public void AllActions_HaveValidSelectors()
    {
        // Act & Assert
        foreach (var kvp in GameActions.All)
        {
            Assert.False(string.IsNullOrEmpty(kvp.Value.Selector),
                $"Action '{kvp.Key}' has empty selector");
            Assert.True(kvp.Value.Selector.StartsWith("#"),
                $"Action '{kvp.Key}' selector should start with '#'");
        }
    }

    [Fact]
    public void AllActions_HaveDescriptions()
    {
        // Act & Assert
        foreach (var kvp in GameActions.All)
        {
            Assert.False(string.IsNullOrEmpty(kvp.Value.Description),
                $"Action '{kvp.Key}' has no description");
        }
    }
}

public class GameActionDefinitionTests
{
    [Fact]
    public void GameActionDefinition_DefaultClickCount_IsOne()
    {
        // Arrange & Act
        var action = new GameActionDefinition("Test", "#test", "Test description");

        // Assert
        Assert.Equal(1, action.ClickCount);
    }

    [Fact]
    public void GameActionDefinition_DefaultIsSlider_IsFalse()
    {
        // Arrange & Act
        var action = new GameActionDefinition("Test", "#test", "Test description");

        // Assert
        Assert.False(action.IsSlider);
    }

    [Fact]
    public void GameActionDefinition_DefaultSliderValue_IsZero()
    {
        // Arrange & Act
        var action = new GameActionDefinition("Test", "#test", "Test description");

        // Assert
        Assert.Equal(0, action.SliderValue);
    }

    [Fact]
    public void GameActionDefinition_WithCustomValues_SetsCorrectly()
    {
        // Arrange & Act
        var action = new GameActionDefinition(
            "CustomAction",
            "#customBtn",
            "Custom description",
            ClickCount: 5,
            IsSlider: true,
            SliderValue: 7);

        // Assert
        Assert.Equal("CustomAction", action.Name);
        Assert.Equal("#customBtn", action.Selector);
        Assert.Equal("Custom description", action.Description);
        Assert.Equal(5, action.ClickCount);
        Assert.True(action.IsSlider);
        Assert.Equal(7, action.SliderValue);
    }
}
