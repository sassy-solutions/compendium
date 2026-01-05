namespace Compendium.Adapters.OpenRouter.Tests.Configuration;

public class OpenRouterOptionsTests
{
    [Fact]
    public void OpenRouterOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new OpenRouterOptions();

        // Assert
        options.ApiKey.Should().BeEmpty();
        options.BaseUrl.Should().Be("https://openrouter.ai/api/v1");
        options.DefaultModel.Should().Be("anthropic/claude-3.5-sonnet");
        options.DefaultTemperature.Should().Be(0.7f);
        options.DefaultMaxTokens.Should().Be(4096);
        options.TimeoutSeconds.Should().Be(120);
        options.RetryAttempts.Should().Be(3);
        options.EnableLogging.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithApiKey_ShouldReturnTrue()
    {
        // Arrange
        var options = new OpenRouterOptions
        {
            ApiKey = "sk-or-v1-test-key"
        };

        // Act & Assert
        options.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithoutApiKey_ShouldReturnFalse()
    {
        // Arrange
        var options = new OpenRouterOptions();

        // Act & Assert
        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithEmptyApiKey_ShouldReturnFalse()
    {
        // Arrange
        var options = new OpenRouterOptions
        {
            ApiKey = ""
        };

        // Act & Assert
        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithWhitespaceApiKey_ShouldReturnFalse()
    {
        // Arrange
        var options = new OpenRouterOptions
        {
            ApiKey = "   "
        };

        // Act & Assert
        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void OpenRouterOptions_WithCustomValues_ShouldRetainValues()
    {
        // Arrange & Act
        var options = new OpenRouterOptions
        {
            ApiKey = "my-api-key",
            BaseUrl = "https://custom.api.com",
            DefaultModel = "openai/gpt-4o",
            DefaultTemperature = 0.5f,
            DefaultMaxTokens = 8192,
            TimeoutSeconds = 60,
            RetryAttempts = 5,
            SiteUrl = "https://mysite.com",
            SiteName = "My App",
            EnableLogging = true
        };

        // Assert
        options.ApiKey.Should().Be("my-api-key");
        options.BaseUrl.Should().Be("https://custom.api.com");
        options.DefaultModel.Should().Be("openai/gpt-4o");
        options.DefaultTemperature.Should().Be(0.5f);
        options.DefaultMaxTokens.Should().Be(8192);
        options.TimeoutSeconds.Should().Be(60);
        options.RetryAttempts.Should().Be(5);
        options.SiteUrl.Should().Be("https://mysite.com");
        options.SiteName.Should().Be("My App");
        options.EnableLogging.Should().BeTrue();
    }

    [Fact]
    public void OpenRouterOptions_ModelConfigs_ShouldAllowCustomModelSettings()
    {
        // Arrange & Act
        var options = new OpenRouterOptions
        {
            ApiKey = "test",
            Models = new Dictionary<string, ModelConfig>
            {
                ["anthropic/claude-3-opus"] = new ModelConfig
                {
                    MaxTokens = 4096,
                    Temperature = 0.3f
                },
                ["openai/gpt-4-turbo"] = new ModelConfig
                {
                    MaxTokens = 128000,
                    Temperature = 0.7f
                }
            }
        };

        // Assert
        options.Models.Should().HaveCount(2);
        options.Models["anthropic/claude-3-opus"].MaxTokens.Should().Be(4096);
        options.Models["anthropic/claude-3-opus"].Temperature.Should().Be(0.3f);
        options.Models["openai/gpt-4-turbo"].MaxTokens.Should().Be(128000);
    }
}
