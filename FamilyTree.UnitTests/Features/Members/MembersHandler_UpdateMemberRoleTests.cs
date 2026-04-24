using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Shared;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Members;

public class MembersHandler_UpdateMemberRoleTests
{
    // Rules:
    // - Board must exist and caller must be a member (role is null → BoardNotFound)
    // - Only the board owner can update roles (role != Owner → Forbidden)
    // - Target member must exist (null → MemberNotFound)
    // - Owner cannot update their own role (targetMember.UserId == callerId → CannotEditSelf)
    // - Concurrent deletion of the target member returns MemberNotFound (UpdateMemberRoleAsync → null)

    private static readonly Guid CallerId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IMembersRepository> _repoMock = new();
    private readonly MembersHandler _handler;

    public MembersHandler_UpdateMemberRoleTests()
    {
        _handler = new MembersHandler(_repoMock.Object);
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenBoardNotFound_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.BoardNotFound");
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenCallerIsNotOwner_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Editor);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.Forbidden");
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenTargetMemberNotFound_ShouldReturnMemberNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync((Member?)null);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.MemberNotFound");
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenCallerTriesToEditSelf_ShouldReturnCannotEditSelfError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        var targetMember = new Member(memberId, CallerId, "Self", "User", "self@example.com", BoardRole.Owner, ViewerPrivacyMode.Restricted, DateTime.UtcNow);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync(targetMember);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.CannotEditSelf");
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenUpdateReturnsNull_ShouldReturnMemberNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        var targetMember = new Member(memberId, targetUserId, "Other", "User", "other@example.com", BoardRole.Editor, ViewerPrivacyMode.Restricted, DateTime.UtcNow);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync(targetMember);

        _repoMock
            .Setup(r => r.UpdateMemberRoleAsync(boardId, memberId, BoardRole.Viewer))
            .ReturnsAsync((Member?)null);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.MemberNotFound");
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenAllConditionsMet_ShouldUpdateAndReturnMemberResponse()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        var targetMember = new Member(memberId, targetUserId, "Other", "User", "other@example.com", BoardRole.Editor, ViewerPrivacyMode.Restricted, createdAt);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync(targetMember);

        var updatedRow = new Member(memberId, targetUserId, "Other", "User", "other@example.com", BoardRole.Viewer, ViewerPrivacyMode.Restricted, createdAt);

        _repoMock
            .Setup(r => r.UpdateMemberRoleAsync(boardId, memberId, BoardRole.Viewer))
            .ReturnsAsync(updatedRow);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), CallerId);

        result.IsError.Should().BeFalse();
        result.Value.MemberId.Should().Be(memberId);
        result.Value.Role.Should().Be(BoardRole.Viewer);
    }
}
