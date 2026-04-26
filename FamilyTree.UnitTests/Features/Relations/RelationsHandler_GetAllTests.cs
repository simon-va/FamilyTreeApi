using System.Data;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Relations;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Relations;

public class RelationsHandler_GetAllTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - All members (Owner, Editor, Viewer) may read

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IRelationsRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IMembersRepository> _memberRepoMock = new();
    private readonly RelationsHandler _handler;

    public RelationsHandler_GetAllTests()
    {
        _handler = new RelationsHandler(
            _repoMock.Object, _fuzzyDateRepoMock.Object,
            _connectionFactoryMock.Object, _memberRepoMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_WhenCallerIsNotBoardMember_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.GetAllAsync(boardId, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Relations.BoardNotFound");
    }

    [Theory]
    [InlineData(BoardRole.Owner)]
    [InlineData(BoardRole.Editor)]
    [InlineData(BoardRole.Viewer)]
    public async Task GetAllAsync_WhenCallerIsMember_ShouldReturnMappedRelationList(BoardRole role)
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var relation = new Relation(relationId, boardId, Guid.NewGuid(), Guid.NewGuid(),
            RelationType.Spouse, createdAt, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(role);

        _repoMock
            .Setup(r => r.GetAllAsync(boardId))
            .ReturnsAsync(new[] { (relation, (FuzzyDate?)null, (FuzzyDate?)null) });

        var result = await _handler.GetAllAsync(boardId, UserId);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(relationId);
        result.Value[0].Type.Should().Be(RelationType.Spouse);
        result.Value[0].StartDate.Should().BeNull();
        result.Value[0].EndDate.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenRelationHasStartDate_ShouldReturnStartDateInResponse()
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();
        var startDateId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var relation = new Relation(relationId, boardId, Guid.NewGuid(), Guid.NewGuid(),
            RelationType.Spouse, createdAt, startDateId, null, null, null);

        var startDate = new FuzzyDate(startDateId, FuzzyDatePrecision.Year, new DateOnly(2000, 1, 1),
            null, null, null, null, createdAt);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetAllAsync(boardId))
            .ReturnsAsync(new[] { (relation, (FuzzyDate?)startDate, (FuzzyDate?)null) });

        var result = await _handler.GetAllAsync(boardId, UserId);

        result.IsError.Should().BeFalse();
        var startDate2 = result.Value[0].StartDate;
        startDate2.Should().NotBeNull();
        startDate2!.Precision.Should().Be(FuzzyDatePrecision.Year);
        startDate2.Date.Should().Be(new DateOnly(2000, 1, 1));
        result.Value[0].EndDate.Should().BeNull();
    }
}
