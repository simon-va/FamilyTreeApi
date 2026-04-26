using FluentValidation;
using FamilyTreeApiV2.Features.Persons;

namespace FamilyTreeApiV2.Features.Residences;

public class CreateResidenceRequestValidator : AbstractValidator<CreateResidenceRequest>
{
    public CreateResidenceRequestValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("'PersonId' is required.");

        RuleFor(x => x.City)
            .MaximumLength(200).WithMessage("'City' must not exceed 200 characters.")
            .When(x => x.City is not null);

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("'Country' must not exceed 100 characters.")
            .When(x => x.Country is not null);

        RuleFor(x => x.Street)
            .MaximumLength(200).WithMessage("'Street' must not exceed 200 characters.")
            .When(x => x.Street is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("'Notes' must not exceed 2000 characters.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.StartDate)
            .SetValidator(new FuzzyDateRequestValidator()!)
            .When(x => x.StartDate is not null);

        RuleFor(x => x.EndDate)
            .SetValidator(new FuzzyDateRequestValidator()!)
            .When(x => x.EndDate is not null);
    }
}

public class UpdateResidenceRequestValidator : AbstractValidator<UpdateResidenceRequest>
{
    public UpdateResidenceRequestValidator()
    {
        RuleFor(x => x.City)
            .MaximumLength(200).WithMessage("'City' must not exceed 200 characters.")
            .When(x => x.City is not null);

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("'Country' must not exceed 100 characters.")
            .When(x => x.Country is not null);

        RuleFor(x => x.Street)
            .MaximumLength(200).WithMessage("'Street' must not exceed 200 characters.")
            .When(x => x.Street is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("'Notes' must not exceed 2000 characters.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.StartDate)
            .SetValidator(new FuzzyDateRequestValidator()!)
            .When(x => x.StartDate is not null);

        RuleFor(x => x.EndDate)
            .SetValidator(new FuzzyDateRequestValidator()!)
            .When(x => x.EndDate is not null);
    }
}
