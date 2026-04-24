using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Members;

public record AddMemberRequest(string Email, BoardRole Role);
public record UpdateMemberRoleRequest(BoardRole Role);
public record UpdateViewerPrivacyModeRequest(ViewerPrivacyMode ViewerPrivacyMode);

public record Member(
    Guid MemberId,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    BoardRole Role,
    ViewerPrivacyMode? ViewerPrivacyMode,
    DateTime CreatedAt);

public record MemberResponse(
    Guid MemberId,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    BoardRole Role,
    ViewerPrivacyMode? ViewerPrivacyMode,
    DateTime CreatedAt);
