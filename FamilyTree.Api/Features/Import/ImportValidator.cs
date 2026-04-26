using FluentValidation;

namespace FamilyTreeApiV2.Features.Import;

public class ImportRequestValidator : AbstractValidator<ImportRequest>
{
    public ImportRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("'Username' is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("'Password' is required.");
    }
}
