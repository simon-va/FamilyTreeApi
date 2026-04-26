using FluentValidation;
using FamilyTreeApiV2.Features.Persons;

namespace FamilyTreeApiV2.Features.Relations;

public class CreateRelationRequestValidator : AbstractValidator<CreateRelationRequest>
{
    public CreateRelationRequestValidator()
    {
        RuleFor(x => x.PersonBId)
            .Must((dto, personBId) => personBId != dto.PersonAId)
            .WithMessage("'PersonBId' must differ from 'PersonAId'.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("'Type' is not a valid relation type.");

        RuleFor(x => x.StartDate)
            .SetValidator(new FuzzyDateRequestValidator()!)
            .When(x => x.StartDate is not null);

        RuleFor(x => x.EndDate)
            .SetValidator(new FuzzyDateRequestValidator()!)
            .When(x => x.EndDate is not null);

        RuleFor(x => x.EndReason)
            .MaximumLength(500).WithMessage("End reason must not exceed 500 characters.")
            .When(x => x.EndReason is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.")
            .When(x => x.Notes is not null);
    }
}

public class UpdateRelationRequestValidator : AbstractValidator<UpdateRelationRequest>
{
    public UpdateRelationRequestValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("'Type' is not a valid relation type.");

        RuleFor(x => x.StartDate)
            .SetValidator(new FuzzyDateRequestValidator()!)
            .When(x => x.StartDate is not null);

        RuleFor(x => x.EndDate)
            .SetValidator(new FuzzyDateRequestValidator()!)
            .When(x => x.EndDate is not null);

        RuleFor(x => x.EndReason)
            .MaximumLength(500).WithMessage("End reason must not exceed 500 characters.")
            .When(x => x.EndReason is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.")
            .When(x => x.Notes is not null);
    }
}
