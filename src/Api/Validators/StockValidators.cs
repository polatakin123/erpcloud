using ErpCloud.Api.Models;
using FluentValidation;

namespace ErpCloud.Api.Validators;

public class ReceiveStockValidator : AbstractValidator<ReceiveStockDto>
{
    public ReceiveStockValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required.");
        
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required.");
        
        RuleFor(x => x.Qty)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
        
        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UnitCost.HasValue)
            .WithMessage("UnitCost must be non-negative.");
        
        RuleFor(x => x.ReferenceType)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.ReferenceType))
            .WithMessage("ReferenceType must be 64 characters or less.");
        
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Note))
            .WithMessage("Note must be 500 characters or less.");
    }
}

public class IssueStockValidator : AbstractValidator<IssueStockDto>
{
    public IssueStockValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required.");
        
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required.");
        
        RuleFor(x => x.Qty)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
        
        RuleFor(x => x.ReferenceType)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.ReferenceType))
            .WithMessage("ReferenceType must be 64 characters or less.");
        
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Note))
            .WithMessage("Note must be 500 characters or less.");
    }
}

public class ReserveStockValidator : AbstractValidator<ReserveStockDto>
{
    public ReserveStockValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required.");
        
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required.");
        
        RuleFor(x => x.Qty)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
        
        RuleFor(x => x.ReferenceType)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.ReferenceType))
            .WithMessage("ReferenceType must be 64 characters or less.");
        
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Note))
            .WithMessage("Note must be 500 characters or less.");
    }
}

public class ReleaseReservationValidator : AbstractValidator<ReleaseReservationDto>
{
    public ReleaseReservationValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required.");
        
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required.");
        
        RuleFor(x => x.Qty)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
        
        RuleFor(x => x.ReferenceType)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.ReferenceType))
            .WithMessage("ReferenceType must be 64 characters or less.");
        
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Note))
            .WithMessage("Note must be 500 characters or less.");
    }
}

public class TransferStockValidator : AbstractValidator<TransferStockDto>
{
    public TransferStockValidator()
    {
        RuleFor(x => x.FromWarehouseId)
            .NotEmpty()
            .WithMessage("FromWarehouseId is required.");
        
        RuleFor(x => x.ToWarehouseId)
            .NotEmpty()
            .WithMessage("ToWarehouseId is required.");
        
        RuleFor(x => x.FromWarehouseId)
            .NotEqual(x => x.ToWarehouseId)
            .WithMessage("FromWarehouseId and ToWarehouseId must be different.");
        
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required.");
        
        RuleFor(x => x.Qty)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
        
        RuleFor(x => x.ReferenceType)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.ReferenceType))
            .WithMessage("ReferenceType must be 64 characters or less.");
        
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Note))
            .WithMessage("Note must be 500 characters or less.");
    }
}

public class AdjustStockValidator : AbstractValidator<AdjustStockDto>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required.");
        
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required.");
        
        RuleFor(x => x.Qty)
            .NotEqual(0)
            .WithMessage("Adjustment quantity cannot be zero.");
        
        RuleFor(x => x.ReferenceType)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.ReferenceType))
            .WithMessage("ReferenceType must be 64 characters or less.");
        
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Note))
            .WithMessage("Note must be 500 characters or less.");
    }
}
