using FluentValidation;
using ErpCloud.Api.Models;
using System.Text.RegularExpressions;

namespace ErpCloud.Api.Validators;

public class CreateSalesOrderDtoValidator : AbstractValidator<CreateSalesOrderDto>
{
    public CreateSalesOrderDtoValidator()
    {
        RuleFor(x => x.OrderNo)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode)
            .WithMessage("OrderNo must contain only uppercase letters, numbers, underscore and hyphen");

        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.OrderDate).NotEmpty();

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note != null);

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("At least one line is required");

        RuleForEach(x => x.Lines)
            .SetValidator(new CreateSalesOrderLineDtoValidator());
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return false;
            
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}

public class CreateSalesOrderLineDtoValidator : AbstractValidator<CreateSalesOrderLineDto>
{
    public CreateSalesOrderLineDtoValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();

        RuleFor(x => x.Qty)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UnitPrice.HasValue)
            .WithMessage("UnitPrice must be >= 0");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100)
            .When(x => x.VatRate.HasValue)
            .WithMessage("VatRate must be between 0 and 100");

        RuleFor(x => x.Note)
            .MaximumLength(200)
            .When(x => x.Note != null);
    }
}

public class UpdateSalesOrderDtoValidator : AbstractValidator<UpdateSalesOrderDto>
{
    public UpdateSalesOrderDtoValidator()
    {
        RuleFor(x => x.OrderNo)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode)
            .WithMessage("OrderNo must contain only uppercase letters, numbers, underscore and hyphen");

        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.OrderDate).NotEmpty();

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note != null);

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("At least one line is required");

        RuleForEach(x => x.Lines)
            .SetValidator(new UpdateSalesOrderLineDtoValidator());
    }

    private bool BeValidCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return false;
            
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }
}

public class UpdateSalesOrderLineDtoValidator : AbstractValidator<UpdateSalesOrderLineDto>
{
    public UpdateSalesOrderLineDtoValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();

        RuleFor(x => x.Qty)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UnitPrice.HasValue)
            .WithMessage("UnitPrice must be >= 0");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100)
            .When(x => x.VatRate.HasValue)
            .WithMessage("VatRate must be between 0 and 100");

        RuleFor(x => x.Note)
            .MaximumLength(200)
            .When(x => x.Note != null);
    }
}
