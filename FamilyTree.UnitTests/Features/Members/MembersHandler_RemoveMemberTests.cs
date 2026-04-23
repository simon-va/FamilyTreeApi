using ErrorOr;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Shared;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Members;

public class MembersHandler_RemoveMemberTests
{
    // Rules:
    // - Board must exist and caller must be a member (role is null → BoardNotFound)
    // - Only the board owner can remove members (role != Owner → Forbidden)
    // - Target member must exist (null → MemberNotFound)
    // - Owner cannot remove themselves (targetMember.UserId == callerId → CannotRemoveSelf)
    // - Concurrent deletion of the target member returns MemberNotFound (DeleteMemberAsync → false)

    private static readonly Guid CallerId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IMembersRepository> _repoMock = new();
    private readonly MembersHandler _handler;

    public MembersHandler_RemoveMemberTests()
    {
        _handler = new MembersHandler(_repoMock.Object);
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenBoardNotFound_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.RemoveMemberAsync(boardId, memberId, CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.BoardNotFound");
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenCallerIsNotOwner_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Editor);

        var result = await _handler.RemoveMemberAsync(boardId, memberId, CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.Forbidden");
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenTargetMemberNotFound_ShouldReturnMemberNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync((Member?)null);

        var result = await _handler.RemoveMemberAsync(boardId, memberId, CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.MemberNotFound");
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenCallerTriesToRemoveSelf_ShouldReturnCannotRemoveSelfError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        var targetMember = new Member(memberId, CallerId, "Self", "User", "self@example.com", BoardRole.Owner, DateTime.UtcNow);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync(targetMember);

        var result = await _handler.RemoveMemberAsync(boardId, memberId, CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.CannotRemoveSelf");
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenDeleteReturnsFalse_ShouldReturnMemberNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        var targetMember = new Member(memberId, targetUserId, "Other", "User", "other@example.com", BoardRole.Editor, DateTime.UtcNow);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync(targetMember);

        _repoMock
            .Setup(r => r.DeleteMemberAsync(boardId, memberId))
            .ReturnsAsync(false);

        var result = await _handler.RemoveMemberAsync(boardId, memberId, CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.MemberNotFound");
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenAllConditionsMet_ShouldDeleteAndReturnDeleted()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        var targetMember = new Member(memberId, targetUserId, "Other", "User", "other@example.com", BoardRole.Editor, DateTime.UtcNow);

        _repoMock
            .Setup(r => r.GetMemberByIdAsync(boardId, memberId))
            .ReturnsAsync(targetMember);

        _repoMock
            .Setup(r => r.DeleteMemberAsync(boardId, memberId))
            .ReturnsAsync(true);

        var result = await _handler.RemoveMemberAsync(boardId, memberId, CallerId);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        _repoMock.Verify(r => r.DeleteMemberAsync(boardId, memberId), Times.Once);
    }
}
