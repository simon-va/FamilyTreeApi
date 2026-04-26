using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Relations;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Relations;

public class RelationsHandler_DeleteTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - Only Owner and Editor may delete (Viewer → Forbidden)
    // - Relation must exist (false from DeleteAsync → RelationNotFound)

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IRelationsRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IMembersRepository> _memberRepoMock = new();
    private readonly RelationsHandler _handler;

    public RelationsHandler_DeleteTests()
    {
        _handler = new RelationsHandler(
            _repoMock.Object, _fuzzyDateRepoMock.Object,
            _connectionFactoryMock.Object, _memberRepoMock.Object);
    }

    [Fact]
    public async Task DeleteAsync_WhenCallerIsNotBoardMember_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.DeleteAsync(boardId, relationId, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Relations.BoardNotFound");
    }

    [Fact]
    public async Task DeleteAsync_WhenCallerIsViewer_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Viewer);

        var result = await _handler.DeleteAsync(boardId, relationId, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Relations.Forbidden");
    }

    [Fact]
    public async Task DeleteAsync_WhenRelationDoesNotExist_ShouldReturnRelationNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Editor);

        _repoMock
            .Setup(r => r.DeleteAsync(boardId, relationId))
            .ReturnsAsync(false);

        var result = await _handler.DeleteAsync(boardId, relationId, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Relations.RelationNotFound");
    }

    [Theory]
    [InlineData(BoardRole.Owner)]
    [InlineData(BoardRole.Editor)]
    public async Task DeleteAsync_WhenCallerIsOwnerOrEditor_ShouldReturnDeleted(BoardRole role)
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(role);

        _repoMock
            .Setup(r => r.DeleteAsync(boardId, relationId))
            .ReturnsAsync(true);

        var result = await _handler.DeleteAsync(boardId, relationId, UserId);

        result.IsError.Should().BeFalse();
    }
}
