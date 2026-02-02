using FluentValidation;
using ErpCloud.Api.Models;
using System.Text.RegularExpressions;

namespace ErpCloud.Api.Validators;

public class CreatePartyValidator : AbstractValidator<CreatePartyDto>
{
    private static readonly HashSet<string> AllowedTypes = new() { "CUSTOMER", "SUPPLIER", "BOTH" };

    public CreatePartyValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode).WithMessage("Code must contain only A-Z, 0-9, _, -");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => AllowedTypes.Contains(t.ToUpper()))
            .WithMessage("Type must be one of: CUSTOMER, SUPPLIER, BOTH");

        RuleFor(x => x.TaxNumber)
            .MaximumLength(32)
            .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber));

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Address));

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CreditLimit.HasValue)
            .WithMessage("CreditLimit must be greater than or equal to 0");

        RuleFor(x => x.PaymentTermDays)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PaymentTermDays.HasValue)
            .WithMessage("PaymentTermDays must be greater than or equal to 0");
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}

public class UpdatePartyValidator : AbstractValidator<UpdatePartyDto>
{
    private static readonly HashSet<string> AllowedTypes = new() { "CUSTOMER", "SUPPLIER", "BOTH" };

    public UpdatePartyValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode).WithMessage("Code must contain only A-Z, 0-9, _, -");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => AllowedTypes.Contains(t.ToUpper()))
            .WithMessage("Type must be one of: CUSTOMER, SUPPLIER, BOTH");

        RuleFor(x => x.TaxNumber)
            .MaximumLength(32)
            .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber));

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Address));

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CreditLimit.HasValue)
            .WithMessage("CreditLimit must be greater than or equal to 0");

        RuleFor(x => x.PaymentTermDays)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PaymentTermDays.HasValue)
            .WithMessage("PaymentTermDays must be greater than or equal to 0");
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}
