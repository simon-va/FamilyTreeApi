using FluentValidation;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Persons;

public class FuzzyDateInputDtoValidator : AbstractValidator<FuzzyDateInputDto>
{
    public FuzzyDateInputDtoValidator()
    {
        RuleFor(x => x.DateTo)
            .NotNull()
            .WithMessage("'DateTo' is required when Precision is 'Between'.")
            .When(x => x.Precision == FuzzyDatePrecision.Between);

        RuleFor(x => x.DateTo)
            .Null()
            .WithMessage("'DateTo' must be null when Precision is not 'Between'.")
            .When(x => x.Precision != FuzzyDatePrecision.Between);

        RuleFor(x => x.DateTo)
            .Must((dto, dateTo) => dateTo!.Value > dto.Date)
            .WithMessage("'DateTo' must be after 'Date' when Precision is 'Between'.")
            .When(x => x.Precision == FuzzyDatePrecision.Between && x.DateTo is not null);

        RuleFor(x => x.Note).MaximumLength(1000);

        RuleFor(x => x.Date)
            .Must(date => date <= DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date))
            .WithMessage("'Date' must not be in the future.");

        RuleFor(x => x.DateTo)
            .Must(dateTo => dateTo!.Value <= DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date))
            .WithMessage("'DateTo' must not be in the future.")
            .When(x => x.DateTo is not null);
    }
}

public class CreatePersonRequestValidator : AbstractValidator<CreatePersonRequest>
{
    public CreatePersonRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.MiddleNames)
            .MaximumLength(200).WithMessage("Middle names must not exceed 200 characters.")
            .When(x => x.MiddleNames is not null);

        RuleFor(x => x.BirthName)
            .MaximumLength(100).WithMessage("Birth name must not exceed 100 characters.")
            .When(x => x.BirthName is not null);

        RuleFor(x => x.BirthPlace)
            .MaximumLength(200).WithMessage("Birth place must not exceed 200 characters.")
            .When(x => x.BirthPlace is not null);

        RuleFor(x => x.BirthDate)
            .SetValidator(new FuzzyDateInputDtoValidator()!)
            .When(x => x.BirthDate is not null);

        RuleFor(x => x.DeathPlace)
            .MaximumLength(200).WithMessage("Death place must not exceed 200 characters.")
            .When(x => x.DeathPlace is not null);

        RuleFor(x => x.DeathDate)
            .SetValidator(new FuzzyDateInputDtoValidator()!)
            .When(x => x.DeathDate is not null);

        RuleFor(x => x.BurialPlace)
            .MaximumLength(200).WithMessage("Burial place must not exceed 200 characters.")
            .When(x => x.BurialPlace is not null);

        RuleFor(x => x.Title)
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.")
            .When(x => x.Title is not null);

        RuleFor(x => x.Religion)
            .MaximumLength(100).WithMessage("Religion must not exceed 100 characters.")
            .When(x => x.Religion is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.")
            .When(x => x.Notes is not null);
    }
}

public class UpdatePersonRequestValidator : AbstractValidator<UpdatePersonRequest>
{
    public UpdatePersonRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.MiddleNames)
            .MaximumLength(200).WithMessage("Middle names must not exceed 200 characters.")
            .When(x => x.MiddleNames is not null);

        RuleFor(x => x.BirthName)
            .MaximumLength(100).WithMessage("Birth name must not exceed 100 characters.")
            .When(x => x.BirthName is not null);

        RuleFor(x => x.BirthPlace)
            .MaximumLength(200).WithMessage("Birth place must not exceed 200 characters.")
            .When(x => x.BirthPlace is not null);

        RuleFor(x => x.BirthDate)
            .SetValidator(new FuzzyDateInputDtoValidator()!)
            .When(x => x.BirthDate is not null);

        RuleFor(x => x.DeathPlace)
            .MaximumLength(200).WithMessage("Death place must not exceed 200 characters.")
            .When(x => x.DeathPlace is not null);

        RuleFor(x => x.DeathDate)
            .SetValidator(new FuzzyDateInputDtoValidator()!)
            .When(x => x.DeathDate is not null);

        RuleFor(x => x.BurialPlace)
            .MaximumLength(200).WithMessage("Burial place must not exceed 200 characters.")
            .When(x => x.BurialPlace is not null);

        RuleFor(x => x.Title)
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.")
            .When(x => x.Title is not null);

        RuleFor(x => x.Religion)
            .MaximumLength(100).WithMessage("Religion must not exceed 100 characters.")
            .When(x => x.Religion is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.")
            .When(x => x.Notes is not null);
    }
}
