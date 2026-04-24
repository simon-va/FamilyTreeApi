using ErrorOr;

namespace FamilyTreeApiV2.Features.Persons;

public static class PersonsErrors
{
    public static Error BoardNotFound =>
        Error.NotFound("Persons.BoardNotFound", "Board not found or you are not a member.");

    public static Error Forbidden =>
        Error.Forbidden("Persons.Forbidden", "Only owners and editors can perform this action.");

    public static Error PersonNotFound =>
        Error.NotFound("Persons.PersonNotFound", "Person not found.");
}
