using System.Text.RegularExpressions;

namespace UserService.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="string"/> class.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Removes block and line comments from a JSON string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The string with comments removed.</returns>
    public static string RemoveJsonComments(this string input)
    {
        // Removes block comments /* */
        var noBlockComments = Regex.Replace(input, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

        // Removes line comments //
        var noLineComments = Regex.Replace(noBlockComments, @"//.*(?=\r?$)", string.Empty, RegexOptions.Multiline);
        return noLineComments;
    }
}