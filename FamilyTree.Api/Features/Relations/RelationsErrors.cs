using ErrorOr;

namespace FamilyTreeApiV2.Features.Relations;

public static class RelationsErrors
{
    public static Error BoardNotFound =>
        Error.NotFound("Relations.BoardNotFound", "Board not found or you are not a member.");

    public static Error Forbidden =>
        Error.Forbidden("Relations.Forbidden", "Only owners and editors can perform this action.");

    public static Error PersonNotFound =>
        Error.NotFound("Relations.PersonNotFound", "One or both persons not found on this board.");

    public static Error RelationNotFound =>
        Error.NotFound("Relations.RelationNotFound", "Relation not found on this board.");
}
