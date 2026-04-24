using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Shared;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Members;

public class MembersHandler_GetMembersTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)

    private static readonly Guid CallerId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IMembersRepository> _repoMock = new();
    private readonly MembersHandler _handler;

    public MembersHandler_GetMembersTests()
    {
        _handler = new MembersHandler(_repoMock.Object);
    }

    [Fact]
    public async Task GetMembersAsync_WhenCallerIsNotBoardMember_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.GetMembersAsync(boardId, CallerId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Members.BoardNotFound");
    }

    [Fact]
    public async Task GetMembersAsync_WhenCallerIsMember_ShouldReturnMappedMemberList()
    {
        var boardId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var rows = new[]
        {
            new Member(memberId, userId, "Anna", "Müller", "anna@example.com", BoardRole.Editor, null, createdAt)
        };

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, CallerId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetMembersAsync(boardId))
            .ReturnsAsync(rows);

        var result = await _handler.GetMembersAsync(boardId, CallerId);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value[0].MemberId.Should().Be(memberId);
        result.Value[0].UserId.Should().Be(userId);
        result.Value[0].Email.Should().Be("anna@example.com");
        result.Value[0].Role.Should().Be(BoardRole.Editor);
    }
}
