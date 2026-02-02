using FluentValidation;
using ErpCloud.Api.Models;
using System.Text.RegularExpressions;

namespace ErpCloud.Api.Validators;

public class CreateOrganizationValidator : AbstractValidator<CreateOrganizationDto>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode).WithMessage("Code must contain only A-Z, 0-9, _, -");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.TaxNumber)
            .MaximumLength(32)
            .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber));
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}

public class UpdateOrganizationValidator : AbstractValidator<UpdateOrganizationDto>
{
    public UpdateOrganizationValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode).WithMessage("Code must contain only A-Z, 0-9, _, -");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.TaxNumber)
            .MaximumLength(32)
            .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber));
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}

public class CreateBranchValidator : AbstractValidator<CreateBranchDto>
{
    public CreateBranchValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode).WithMessage("Code must contain only A-Z, 0-9, _, -");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.City)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Address));
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}

public class UpdateBranchValidator : AbstractValidator<UpdateBranchDto>
{
    public UpdateBranchValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode).WithMessage("Code must contain only A-Z, 0-9, _, -");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.City)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Address));
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}

public class CreateWarehouseValidator : AbstractValidator<CreateWarehouseDto>
{
    private static readonly HashSet<string> AllowedTypes = new() { "MAIN", "STORE", "VIRTUAL" };

    public CreateWarehouseValidator()
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
            .WithMessage("Type must be one of: MAIN, STORE, VIRTUAL");
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}

public class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseDto>
{
    private static readonly HashSet<string> AllowedTypes = new() { "MAIN", "STORE", "VIRTUAL" };

    public UpdateWarehouseValidator()
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
            .WithMessage("Type must be one of: MAIN, STORE, VIRTUAL");
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}
