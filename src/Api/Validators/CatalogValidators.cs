using FluentValidation;
using ErpCloud.Api.Models;
using System.Text.RegularExpressions;

namespace ErpCloud.Api.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode).WithMessage("Code must contain only A-Z, 0-9, _, -");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}

public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode).WithMessage("Code must contain only A-Z, 0-9, _, -");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}

public class CreateProductVariantValidator : AbstractValidator<CreateProductVariantDto>
{
    public CreateProductVariantValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .Length(2, 64);

        RuleFor(x => x.Barcode)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.Barcode));

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.Unit)
            .NotEmpty()
            .MaximumLength(16);

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100)
            .WithMessage("VatRate must be between 0 and 100");
    }
}

public class UpdateProductVariantValidator : AbstractValidator<UpdateProductVariantDto>
{
    public UpdateProductVariantValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .Length(2, 64);

        RuleFor(x => x.Barcode)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.Barcode));

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.Unit)
            .NotEmpty()
            .MaximumLength(16);

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100)
            .WithMessage("VatRate must be between 0 and 100");
    }
}

public class CreatePriceListValidator : AbstractValidator<CreatePriceListDto>
{
    public CreatePriceListValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode).WithMessage("Code must contain only A-Z, 0-9, _, -");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3, 3)
            .Must(BeUpperCase).WithMessage("Currency must be 3 uppercase letters");
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }

    private bool BeUpperCase(string currency)
    {
        return currency == currency.ToUpper();
    }
}

public class UpdatePriceListValidator : AbstractValidator<UpdatePriceListDto>
{
    public UpdatePriceListValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode).WithMessage("Code must contain only A-Z, 0-9, _, -");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3, 3)
            .Must(BeUpperCase).WithMessage("Currency must be 3 uppercase letters");
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }

    private bool BeUpperCase(string currency)
    {
        return currency == currency.ToUpper();
    }
}

public class CreatePriceListItemValidator : AbstractValidator<CreatePriceListItemDto>
{
    public CreatePriceListItemValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty();

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("UnitPrice must be greater than or equal to 0");

        RuleFor(x => x.MinQty)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinQty.HasValue)
            .WithMessage("MinQty must be greater than or equal to 0");

        RuleFor(x => x)
            .Must(x => !x.ValidFrom.HasValue || !x.ValidTo.HasValue || x.ValidTo >= x.ValidFrom)
            .WithMessage("ValidTo must be greater than or equal to ValidFrom");
    }
}

public class UpdatePriceListItemValidator : AbstractValidator<UpdatePriceListItemDto>
{
    public UpdatePriceListItemValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty();

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("UnitPrice must be greater than or equal to 0");

        RuleFor(x => x.MinQty)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinQty.HasValue)
            .WithMessage("MinQty must be greater than or equal to 0");

        RuleFor(x => x)
            .Must(x => !x.ValidFrom.HasValue || !x.ValidTo.HasValue || x.ValidTo >= x.ValidFrom)
            .WithMessage("ValidTo must be greater than or equal to ValidFrom");
    }
}
