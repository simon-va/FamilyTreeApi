using ErrorOr;

namespace FamilyTreeApiV2.Features.Members;

public static class MembersErrors
{
    public static Error BoardNotFound =>
        Error.NotFound("Members.BoardNotFound", "Board not found or you are not a member.");

    public static Error Forbidden =>
        Error.Forbidden("Members.Forbidden", "Only the board owner can perform this action.");

    public static Error UserNotFound =>
        Error.NotFound("Members.UserNotFound", "No registered user found with that email address.");

    public static Error AlreadyMember =>
        Error.Conflict("Members.AlreadyMember", "This user is already a member of the board.");

    public static Error MemberNotFound =>
        Error.NotFound("Members.MemberNotFound", "Member not found on this board.");

    public static Error CannotEditSelf =>
        Error.Validation("Members.CannotEditSelf", "You cannot change your own role.");

    public static Error CannotRemoveSelf =>
        Error.Validation("Members.CannotRemoveSelf", "You cannot remove yourself from the board.");
}
