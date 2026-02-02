using FluentValidation;
using ErpCloud.Api.Models;
using System.Text.RegularExpressions;

namespace ErpCloud.Api.Validators;

// ================== INVOICE VALIDATORS ==================

public class CreateInvoiceDtoValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceDtoValidator()
    {
        RuleFor(x => x.InvoiceNo)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode)
            .WithMessage("InvoiceNo must contain only uppercase letters, numbers, underscore and hyphen");

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(x => x == "SALES" || x == "PURCHASE")
            .WithMessage("Type must be SALES or PURCHASE");

        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.IssueDate).NotEmpty();

        RuleFor(x => x.Currency)
            .Length(3, 3)
            .Must(BeUppercase)
            .When(x => x.Currency != null)
            .WithMessage("Currency must be 3 uppercase characters");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note != null);

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("At least one line is required");

        RuleForEach(x => x.Lines)
            .SetValidator(new CreateInvoiceLineDtoValidator());
    }

    private bool BeValidCode(string code)
    {
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }

    private bool BeUppercase(string value)
    {
        return value == value.ToUpperInvariant();
    }
}

public class CreateInvoiceLineDtoValidator : AbstractValidator<CreateInvoiceLineDto>
{
    public CreateInvoiceLineDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Qty)
            .GreaterThan(0)
            .When(x => x.Qty.HasValue)
            .WithMessage("Qty must be greater than 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UnitPrice.HasValue)
            .WithMessage("UnitPrice must be >= 0");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100)
            .WithMessage("VatRate must be between 0 and 100");

        RuleFor(x => x.LineTotal)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LineTotal.HasValue)
            .WithMessage("LineTotal must be >= 0");

        RuleFor(x => x)
            .Must(x => x.LineTotal.HasValue || (x.Qty.HasValue && x.UnitPrice.HasValue))
            .WithMessage("Either provide LineTotal or both Qty and UnitPrice");
    }
}

public class UpdateInvoiceDtoValidator : AbstractValidator<UpdateInvoiceDto>
{
    public UpdateInvoiceDtoValidator()
    {
        RuleFor(x => x.InvoiceNo)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode)
            .WithMessage("InvoiceNo must contain only uppercase letters, numbers, underscore and hyphen");

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(x => x == "SALES" || x == "PURCHASE")
            .WithMessage("Type must be SALES or PURCHASE");

        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.IssueDate).NotEmpty();

        RuleFor(x => x.Currency)
            .Length(3, 3)
            .Must(BeUppercase)
            .When(x => x.Currency != null)
            .WithMessage("Currency must be 3 uppercase characters");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note != null);

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("At least one line is required");

        RuleForEach(x => x.Lines)
            .SetValidator(new UpdateInvoiceLineDtoValidator());
    }

    private bool BeValidCode(string code)
    {
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }

    private bool BeUppercase(string value)
    {
        return value == value.ToUpperInvariant();
    }
}

public class UpdateInvoiceLineDtoValidator : AbstractValidator<UpdateInvoiceLineDto>
{
    public UpdateInvoiceLineDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Qty)
            .GreaterThan(0)
            .When(x => x.Qty.HasValue)
            .WithMessage("Qty must be greater than 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UnitPrice.HasValue)
            .WithMessage("UnitPrice must be >= 0");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100)
            .WithMessage("VatRate must be between 0 and 100");

        RuleFor(x => x.LineTotal)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LineTotal.HasValue)
            .WithMessage("LineTotal must be >= 0");

        RuleFor(x => x)
            .Must(x => x.LineTotal.HasValue || (x.Qty.HasValue && x.UnitPrice.HasValue))
            .WithMessage("Either provide LineTotal or both Qty and UnitPrice");
    }
}

// ================== PAYMENT VALIDATORS ==================

public class CreatePaymentDtoValidator : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentDtoValidator()
    {
        RuleFor(x => x.PaymentNo)
            .NotEmpty()
            .Length(2, 32)
            .Must(BeValidCode)
            .WithMessage("PaymentNo must contain only uppercase letters, numbers, underscore and hyphen");

        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();

        RuleFor(x => x.Direction)
            .NotEmpty()
            .Must(x => x == "IN" || x == "OUT")
            .WithMessage("Direction must be IN or OUT");

        RuleFor(x => x.Method)
            .NotEmpty()
            .Must(x => x == "CASH" || x == "BANK" || x == "CARD" || x == "OTHER")
            .WithMessage("Method must be CASH, BANK, CARD, or OTHER");

        RuleFor(x => x.Currency)
            .Length(3, 3)
            .Must(BeUppercase)
            .When(x => x.Currency != null)
            .WithMessage("Currency must be 3 uppercase characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note != null);
    }

    private bool BeValidCode(string code)
    {
        return Regex.IsMatch(code, @"^[A-Z0-9_-]+$");
    }

    private bool BeUppercase(string value)
    {
        return value == value.ToUpperInvariant();
    }
}
