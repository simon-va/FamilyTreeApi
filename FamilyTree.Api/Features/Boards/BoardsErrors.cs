using ErrorOr;

namespace FamilyTreeApiV2.Features.Boards;

public static class BoardsErrors
{
    public static Error NotFound =>
        Error.NotFound("Boards.NotFound", "Board not found.");

    public static Error Forbidden =>
        Error.Forbidden("Boards.Forbidden", "Only the board owner can delete it.");
}
