using System.Globalization;
using System.Windows.Controls;

namespace KeepMeAlive.Helpers;

/// <summary>
/// A WPF <see cref="ValidationRule"/> that enforces the date/time format
/// <c>dd:MM:yyyy:HH:mm:ss</c> on a bound <see cref="TextBox"/>.
/// </summary>
public class DateTimeFormatValidationRule : ValidationRule
{
    /// <summary>The expected date/time format string.</summary>
    public const string Format = "dd:MM:yyyy:HH:mm:ss";

    /// <summary>
    /// Validates that <paramref name="value"/> can be parsed using <see cref="Format"/>.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="cultureInfo">The culture context (ignored; <see cref="CultureInfo.InvariantCulture"/> is used for consistency).</param>
    /// <returns>
    /// <see cref="ValidationResult.ValidResult"/> when the format matches;
    /// otherwise a <see cref="ValidationResult"/> with <c>IsValid = false</c>
    /// and an error message stating the required format.
    /// </returns>
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        bool ok = DateTime.TryParseExact(
            value?.ToString(),
            Format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);

        return ok
            ? ValidationResult.ValidResult
            : new ValidationResult(false, $"Format required: {Format}");
    }
}
