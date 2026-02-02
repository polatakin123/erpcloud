using ErpCloud.Api.Models;
using FluentValidation;

namespace ErpCloud.Api.Validators;

public class CreateEDocumentDtoValidator : AbstractValidator<CreateEDocumentDto>
{
    public CreateEDocumentDtoValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        
        RuleFor(x => x.DocumentType)
            .NotEmpty()
            .Must(x => x == "EARCHIVE" || x == "EINVOICE")
            .WithMessage("DocumentType must be EARCHIVE or EINVOICE");
        
        RuleFor(x => x.Scenario)
            .Must(x => x == null || x == "BASIC" || x == "COMMERCIAL")
            .WithMessage("Scenario must be BASIC or COMMERCIAL");
    }
}

public class EDocumentQueryValidator : AbstractValidator<EDocumentQuery>
{
    public EDocumentQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        
        RuleFor(x => x.DocumentType)
            .Must(x => x == null || x == "EARCHIVE" || x == "EINVOICE")
            .When(x => !string.IsNullOrEmpty(x.DocumentType));
    }
}
