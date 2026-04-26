using ErrorOr;

namespace FamilyTreeApiV2.Features.Residences;

public static class ResidencesErrors
{
    public static Error BoardNotFound =>
        Error.NotFound("Residences.BoardNotFound", "Board not found or you are not a member.");

    public static Error Forbidden =>
        Error.Forbidden("Residences.Forbidden", "Only owners and editors can perform this action.");

    public static Error PersonNotFound =>
        Error.NotFound("Residences.PersonNotFound", "Person not found.");

    public static Error ResidenceNotFound =>
        Error.NotFound("Residences.ResidenceNotFound", "Residence not found.");
}
