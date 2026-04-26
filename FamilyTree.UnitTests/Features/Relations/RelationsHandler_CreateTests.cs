using System.Data;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Relations;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Relations;

public class RelationsHandler_CreateTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - Only Owner and Editor may create (Viewer → Forbidden)
    // - Both persons must belong to the board (→ PersonNotFound)

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IRelationsRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IDbConnection> _connectionMock = new();
    private readonly Mock<IDbTransaction> _transactionMock = new();
    private readonly Mock<IMembersRepository> _memberRepoMock = new();
    private readonly RelationsHandler _handler;

    public RelationsHandler_CreateTests()
    {
        _connectionMock.Setup(c => c.BeginTransaction()).Returns(_transactionMock.Object);
        _connectionFactoryMock.Setup(f => f.CreateConnection()).Returns(_connectionMock.Object);

        _handler = new RelationsHandler(
            _repoMock.Object, _fuzzyDateRepoMock.Object,
            _connectionFactoryMock.Object, _memberRepoMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenCallerIsNotBoardMember_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var request = new CreateRelationRequest(Guid.NewGuid(), Guid.NewGuid(), RelationType.Spouse, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Relations.BoardNotFound");
    }

    [Fact]
    public async Task CreateAsync_WhenCallerIsViewer_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var request = new CreateRelationRequest(Guid.NewGuid(), Guid.NewGuid(), RelationType.Spouse, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Viewer);

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Relations.Forbidden");
    }

    [Fact]
    public async Task CreateAsync_WhenPersonsDoNotBelongToBoard_ShouldReturnPersonNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var request = new CreateRelationRequest(Guid.NewGuid(), Guid.NewGuid(), RelationType.Spouse, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.DoPersonsBelongToBoardAsync(boardId, request.PersonAId, request.PersonBId))
            .ReturnsAsync(false);

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Relations.PersonNotFound");
    }

    [Theory]
    [InlineData(BoardRole.Owner)]
    [InlineData(BoardRole.Editor)]
    public async Task CreateAsync_WhenCallerIsOwnerOrEditor_ShouldReturnCreatedRelation(BoardRole role)
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();
        var personAId = Guid.NewGuid();
        var personBId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var request = new CreateRelationRequest(personAId, personBId, RelationType.BiologicalParent, null, null, null, null);

        var row = new Relation(relationId, boardId, personAId, personBId, RelationType.BiologicalParent,
            createdAt, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(role);

        _repoMock
            .Setup(r => r.DoPersonsBelongToBoardAsync(boardId, personAId, personBId))
            .ReturnsAsync(true);

        _repoMock
            .Setup(r => r.CreateAsync(boardId, request, null, null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(row);

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(relationId);
        result.Value.PersonAId.Should().Be(personAId);
        result.Value.PersonBId.Should().Be(personBId);
        result.Value.Type.Should().Be(RelationType.BiologicalParent);
        result.Value.StartDate.Should().BeNull();
        result.Value.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIncludesStartDate_ShouldCreateFuzzyDateAndReturnItInResponse()
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();
        var personAId = Guid.NewGuid();
        var personBId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var startDateInput = new FuzzyDateRequest(FuzzyDatePrecision.Year, new DateOnly(2010, 1, 1), null, null, null, null);
        var request = new CreateRelationRequest(personAId, personBId, RelationType.Spouse, startDateInput, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.DoPersonsBelongToBoardAsync(boardId, personAId, personBId))
            .ReturnsAsync(true);

        _fuzzyDateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FuzzyDate>(), _connectionMock.Object, _transactionMock.Object))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.CreateAsync(boardId, request, It.IsAny<Guid?>(), null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync((Guid bId, CreateRelationRequest req, Guid? startDateId, Guid? endDateId, IDbConnection c, IDbTransaction t) =>
                new Relation(relationId, bId, personAId, personBId, RelationType.Spouse,
                    createdAt, startDateId, null, null, null));

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.StartDate.Should().NotBeNull();
        result.Value.StartDate!.Precision.Should().Be(FuzzyDatePrecision.Year);
        result.Value.StartDate.Date.Should().Be(new DateOnly(2010, 1, 1));
        result.Value.EndDate.Should().BeNull();

        _fuzzyDateRepoMock.Verify(r => r.CreateAsync(
            It.Is<FuzzyDate>(d => d.Precision == FuzzyDatePrecision.Year && d.Date == new DateOnly(2010, 1, 1)),
            _connectionMock.Object, _transactionMock.Object), Times.Once);
    }
}
