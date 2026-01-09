using UniversalPaperclipsAI.Utilities;

namespace UniversalPaperclipsAI.Tests.Utilities;

public class StringSanitizerTests
{
    [Fact]
    public void EscapeHtml_WithNull_ReturnsEmpty()
    {
        // Act
        var result = StringSanitizer.EscapeHtml(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EscapeHtml_WithEmpty_ReturnsEmpty()
    {
        // Act
        var result = StringSanitizer.EscapeHtml("");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EscapeHtml_EscapesAllDangerousCharacters()
    {
        // Arrange
        var input = "<script>alert('XSS');</script> & \"test\"";

        // Act
        var result = StringSanitizer.EscapeHtml(input);

        // Assert
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.DoesNotContain("&", result.Replace("&amp;", "").Replace("&lt;", "").Replace("&gt;", "").Replace("&quot;", "").Replace("&#39;", ""));
        Assert.Contains("&lt;", result);
        Assert.Contains("&gt;", result);
        Assert.Contains("&amp;", result);
        Assert.Contains("&quot;", result);
        Assert.Contains("&#39;", result);
    }

    [Fact]
    public void EscapeForJavaScript_WithNull_ReturnsEmpty()
    {
        // Act
        var result = StringSanitizer.EscapeForJavaScript(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EscapeForJavaScript_EscapesTemplateLiteralCharacters()
    {
        // Arrange
        var input = "test `template` with ${expression} and \\backslash";

        // Act
        var result = StringSanitizer.EscapeForJavaScript(input);

        // Assert
        Assert.Contains("\\`", result);
        Assert.Contains("\\${", result);
        Assert.Contains("\\\\", result);
    }

    [Fact]
    public void SanitizeElementId_WithNull_ReturnsEmpty()
    {
        // Act
        var result = StringSanitizer.SanitizeElementId(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeElementId_RemovesInvalidCharacters()
    {
        // Arrange
        var input = "valid-id_123!@#$%^&*()";

        // Act
        var result = StringSanitizer.SanitizeElementId(input);

        // Assert
        Assert.Equal("valid-id_123", result);
    }

    [Fact]
    public void SanitizeElementId_PreservesValidCharacters()
    {
        // Arrange
        var input = "my-element_ID123";

        // Act
        var result = StringSanitizer.SanitizeElementId(input);

        // Assert
        Assert.Equal("my-element_ID123", result);
    }

    [Fact]
    public void Truncate_WithNull_ReturnsEmpty()
    {
        // Act
        var result = StringSanitizer.Truncate(null, 10);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Truncate_WithShortString_ReturnsOriginal()
    {
        // Arrange
        var input = "short";

        // Act
        var result = StringSanitizer.Truncate(input, 10);

        // Assert
        Assert.Equal("short", result);
    }

    [Fact]
    public void Truncate_WithLongString_TruncatesWithEllipsis()
    {
        // Arrange
        var input = "This is a very long string that should be truncated";

        // Act
        var result = StringSanitizer.Truncate(input, 20);

        // Assert
        Assert.Equal(20, result.Length);
        Assert.EndsWith("...", result);
    }

    [Fact]
    public void Truncate_WithExactLength_ReturnsOriginal()
    {
        // Arrange
        var input = "exactly10!";

        // Act
        var result = StringSanitizer.Truncate(input, 10);

        // Assert
        Assert.Equal("exactly10!", result);
    }
}
