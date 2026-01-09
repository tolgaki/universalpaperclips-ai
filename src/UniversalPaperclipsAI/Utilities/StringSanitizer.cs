using System.Text.RegularExpressions;

namespace UniversalPaperclipsAI.Utilities;

/// <summary>
/// Provides string sanitization utilities for safe rendering in HTML and JavaScript contexts.
/// </summary>
public static partial class StringSanitizer
{
    /// <summary>
    /// Escapes HTML special characters to prevent XSS attacks.
    /// </summary>
    /// <param name="input">The string to escape.</param>
    /// <returns>HTML-escaped string safe for rendering in HTML context.</returns>
    public static string EscapeHtml(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    /// <summary>
    /// Sanitizes a string for safe use in JavaScript template literals.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <returns>String safe for use in JavaScript template literals.</returns>
    public static string EscapeForJavaScript(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input
            .Replace("\\", "\\\\")
            .Replace("`", "\\`")
            .Replace("${", "\\${");
    }

    /// <summary>
    /// Sanitizes an element ID to contain only valid characters (alphanumeric, hyphen, underscore).
    /// </summary>
    /// <param name="input">The element ID to sanitize.</param>
    /// <returns>Sanitized element ID.</returns>
    public static string SanitizeElementId(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return ElementIdRegex().Replace(input, "");
    }

    /// <summary>
    /// Truncates a string to a maximum length, adding ellipsis if truncated.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">Maximum length including ellipsis.</param>
    /// <returns>Truncated string.</returns>
    public static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;

        return text[..(maxLength - 3)] + "...";
    }

    [GeneratedRegex("[^a-zA-Z0-9_-]")]
    private static partial Regex ElementIdRegex();
}
