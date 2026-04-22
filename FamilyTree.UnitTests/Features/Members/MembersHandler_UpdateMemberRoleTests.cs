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
            .Setup(r => r.GetCallerRoleAsync(boardId, "user-1"))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), "user-1");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.BoardNotFound");
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenCallerIsNotOwner_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, "user-1"))
            .ReturnsAsync(BoardRole.Editor);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), "user-1");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.Forbidden");
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenTargetMemberNotFound_ShouldReturnMemberNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, "user-1"))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync((MemberRow?)null);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), "user-1");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.MemberNotFound");
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenCallerTriesToEditSelf_ShouldReturnCannotEditSelfError()
    {
        var boardId = Guid.NewGuid();
        var callerGuid = Guid.NewGuid();
        var callerId = callerGuid.ToString();
        var memberId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, callerId))
            .ReturnsAsync(BoardRole.Owner);

        var targetMember = new MemberRow(memberId, callerGuid, "Self", "User", "self@example.com", BoardRole.Owner, DateTime.UtcNow);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync(targetMember);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), callerId);

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
            .Setup(r => r.GetCallerRoleAsync(boardId, "user-1"))
            .ReturnsAsync(BoardRole.Owner);

        var targetMember = new MemberRow(memberId, targetUserId, "Other", "User", "other@example.com", BoardRole.Editor, DateTime.UtcNow);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync(targetMember);

        _repoMock
            .Setup(r => r.UpdateMemberRoleAsync(boardId, memberId, BoardRole.Viewer))
            .ReturnsAsync((MemberRow?)null);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), "user-1");

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
            .Setup(r => r.GetCallerRoleAsync(boardId, "user-1"))
            .ReturnsAsync(BoardRole.Owner);

        var targetMember = new MemberRow(memberId, targetUserId, "Other", "User", "other@example.com", BoardRole.Editor, createdAt);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync(targetMember);

        var updatedRow = new MemberRow(memberId, targetUserId, "Other", "User", "other@example.com", BoardRole.Viewer, createdAt);

        _repoMock
            .Setup(r => r.UpdateMemberRoleAsync(boardId, memberId, BoardRole.Viewer))
            .ReturnsAsync(updatedRow);

        var result = await _handler.UpdateMemberRoleAsync(boardId, memberId, new UpdateMemberRoleRequest(BoardRole.Viewer), "user-1");

        result.IsError.Should().BeFalse();
        result.Value.MemberId.Should().Be(memberId);
        result.Value.Role.Should().Be(BoardRole.Viewer);
    }
}
