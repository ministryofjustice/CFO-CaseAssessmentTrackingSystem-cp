﻿namespace Cfo.Cats.Domain.Entities.ManagementInformation;

public record IneligibilityReason
{
    public string Value { get; }

    private IneligibilityReason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("IneligibilityReason cannot be not, empty, or whitespace.", nameof(value));
        }

        Value = value;
    }

    public static IneligibilityReason MaximumPaymentLimitReached = new("Maximum number of payments reached.");
    public static IneligibilityReason NotYetApproved = new("The enrolment for this participant has not been approved yet.");
    public static IneligibilityReason BeforeConsent = new("Events that take place before the consent date are not eligible for payment.");

}