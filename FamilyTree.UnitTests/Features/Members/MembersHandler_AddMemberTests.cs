using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Shared;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Members;

public class MembersHandler_AddMemberTests
{
    // Rules:
    // - Board must exist and caller must be a member (role is null → BoardNotFound)
    // - Only the board owner can add members (role != Owner → Forbidden)
    // - Target user must exist by email (null → UserNotFound)
    // - Target user must not already be a board member (true → AlreadyMember)

    private static readonly Guid CallerId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IMembersRepository> _repoMock = new();
    private readonly MembersHandler _handler;

    public MembersHandler_AddMemberTests()
    {
        _handler = new MembersHandler(_repoMock.Object);
    }

    [Fact]
    public async Task AddMemberAsync_WhenBoardNotFound_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var request = new AddMemberRequest("new@example.com", BoardRole.Editor);

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.AddMemberAsync(boardId, request, CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.BoardNotFound");
    }

    [Fact]
    public async Task AddMemberAsync_WhenCallerIsNotOwner_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var request = new AddMemberRequest("new@example.com", BoardRole.Editor);

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Editor);

        var result = await _handler.AddMemberAsync(boardId, request, CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.Forbidden");
    }

    [Fact]
    public async Task AddMemberAsync_WhenTargetUserNotFound_ShouldReturnUserNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var request = new AddMemberRequest("ghost@example.com", BoardRole.Editor);

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetUserIdByEmailAsync("ghost@example.com"))
            .ReturnsAsync((Guid?)null);

        var result = await _handler.AddMemberAsync(boardId, request, CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.UserNotFound");
    }

    [Fact]
    public async Task AddMemberAsync_WhenTargetUserAlreadyMember_ShouldReturnAlreadyMemberError()
    {
        var boardId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var request = new AddMemberRequest("existing@example.com", BoardRole.Editor);

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetUserIdByEmailAsync("existing@example.com"))
            .ReturnsAsync(targetUserId);

        _repoMock
            .Setup(r => r.IsMemberAsync(boardId, targetUserId))
            .ReturnsAsync(true);

        var result = await _handler.AddMemberAsync(boardId, request, CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.AlreadyMember");
    }

    [Fact]
    public async Task AddMemberAsync_WhenAllConditionsMet_ShouldAddAndReturnMemberResponse()
    {
        var boardId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var request = new AddMemberRequest("new@example.com", BoardRole.Editor);

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetUserIdByEmailAsync("new@example.com"))
            .ReturnsAsync(targetUserId);

        _repoMock
            .Setup(r => r.IsMemberAsync(boardId, targetUserId))
            .ReturnsAsync(false);

        var row = new Member(memberId, targetUserId, "New", "User", "new@example.com", BoardRole.Editor, ViewerPrivacyMode.Restricted, createdAt);

        _repoMock
            .Setup(r => r.AddMemberAsync(boardId, targetUserId, BoardRole.Editor))
            .ReturnsAsync(row);

        var result = await _handler.AddMemberAsync(boardId, request, CallerId);

        result.IsError.Should().BeFalse();
        result.Value.MemberId.Should().Be(memberId);
        result.Value.UserId.Should().Be(targetUserId);
        result.Value.Email.Should().Be("new@example.com");
        result.Value.Role.Should().Be(BoardRole.Editor);
        _repoMock.Verify(r => r.AddMemberAsync(boardId, targetUserId, BoardRole.Editor), Times.Once);
    }
}
