using UniversalPaperclipsAI.Core;

namespace UniversalPaperclipsAI.Tests.Core;

public class AppConfigurationTests
{
    [Fact]
    public void AppConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new AppConfiguration();

        // Assert
        Assert.NotNull(config.OpenAI);
        Assert.NotNull(config.Game);
        Assert.NotNull(config.Display);
    }
}

public class OpenAISettingsTests
{
    [Fact]
    public void OpenAISettings_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new OpenAISettings();

        // Assert
        Assert.Equal(string.Empty, settings.ApiKey);
        Assert.Equal("gpt-4-turbo", settings.Model);
        Assert.Equal(2000, settings.MaxTokens);
        Assert.Equal(0.7f, settings.Temperature);
        Assert.Equal(1000, settings.MinRequestIntervalMs);
    }

    [Fact]
    public void OpenAISettings_CanSetProperties()
    {
        // Arrange & Act
        var settings = new OpenAISettings
        {
            ApiKey = "test-key",
            Model = "gpt-4o",
            MaxTokens = 4000,
            Temperature = 0.5f,
            MinRequestIntervalMs = 2000
        };

        // Assert
        Assert.Equal("test-key", settings.ApiKey);
        Assert.Equal("gpt-4o", settings.Model);
        Assert.Equal(4000, settings.MaxTokens);
        Assert.Equal(0.5f, settings.Temperature);
        Assert.Equal(2000, settings.MinRequestIntervalMs);
    }

    [Fact]
    public void OpenAISettings_Temperature_RangeValues()
    {
        // Test edge cases for temperature
        var settings = new OpenAISettings();

        settings.Temperature = 0.0f;
        Assert.Equal(0.0f, settings.Temperature);

        settings.Temperature = 1.0f;
        Assert.Equal(1.0f, settings.Temperature);
    }
}

public class GameSettingsTests
{
    [Fact]
    public void GameSettings_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new GameSettings();

        // Assert
        Assert.Equal("https://www.decisionproblem.com/paperclips/index2.html", settings.Url);
        Assert.Equal(500, settings.PollIntervalMs);
        Assert.Equal(3000, settings.DecisionIntervalMs);
        Assert.Equal(5, settings.MaxActionsPerDecision);
    }

    [Fact]
    public void GameSettings_CanSetProperties()
    {
        // Arrange & Act
        var settings = new GameSettings
        {
            Url = "http://localhost:8080/game",
            PollIntervalMs = 1000,
            DecisionIntervalMs = 5000,
            MaxActionsPerDecision = 10
        };

        // Assert
        Assert.Equal("http://localhost:8080/game", settings.Url);
        Assert.Equal(1000, settings.PollIntervalMs);
        Assert.Equal(5000, settings.DecisionIntervalMs);
        Assert.Equal(10, settings.MaxActionsPerDecision);
    }
}

public class DisplaySettingsTests
{
    [Fact]
    public void DisplaySettings_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new DisplaySettings();

        // Assert
        Assert.True(settings.ShowBrowser);
        Assert.True(settings.ShowConsole);
        Assert.True(settings.OverlayEnabled);
    }

    [Fact]
    public void DisplaySettings_CanSetProperties()
    {
        // Arrange & Act
        var settings = new DisplaySettings
        {
            ShowBrowser = false,
            ShowConsole = false,
            OverlayEnabled = false
        };

        // Assert
        Assert.False(settings.ShowBrowser);
        Assert.False(settings.ShowConsole);
        Assert.False(settings.OverlayEnabled);
    }
}
