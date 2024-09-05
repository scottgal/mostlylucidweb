using NodaTime;

namespace Umami.Net.UmamiData.Helpers;

using System;
using System.ComponentModel.DataAnnotations;
using NodaTime;

public class TimeZoneValidatorAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success; // No validation if value is null or empty, can customize as required
        }

        var timeZoneId = value.ToString();

        // Check if the provided timeZoneId exists in the TZDB
        if (DateTimeZoneProviders.Tzdb.Ids.Contains(timeZoneId))
        {
            return ValidationResult.Success; // Valid time zone
        }
        return new ValidationResult($"Invalid time zone ID: {timeZoneId}");
    }
}