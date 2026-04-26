using System.Data;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Relations;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Relations;

public class RelationsHandler_UpdateTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - Only Owner and Editor may update (Viewer → Forbidden)
    // - Relation must exist (null from GetByIdAsync → RelationNotFound)

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IRelationsRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IDbConnection> _connectionMock = new();
    private readonly Mock<IDbTransaction> _transactionMock = new();
    private readonly Mock<IMembersRepository> _memberRepoMock = new();
    private readonly RelationsHandler _handler;

    public RelationsHandler_UpdateTests()
    {
        _connectionMock.Setup(c => c.BeginTransaction()).Returns(_transactionMock.Object);
        _connectionFactoryMock.Setup(f => f.CreateConnection()).Returns(_connectionMock.Object);

        _handler = new RelationsHandler(
            _repoMock.Object, _fuzzyDateRepoMock.Object,
            _connectionFactoryMock.Object, _memberRepoMock.Object);
    }

    [Fact]
    public async Task UpdateAsync_WhenCallerIsNotBoardMember_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();
        var request = new UpdateRelationRequest(RelationType.Spouse, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.UpdateAsync(boardId, relationId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Relations.BoardNotFound");
    }

    [Fact]
    public async Task UpdateAsync_WhenCallerIsViewer_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();
        var request = new UpdateRelationRequest(RelationType.Spouse, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Viewer);

        var result = await _handler.UpdateAsync(boardId, relationId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Relations.Forbidden");
    }

    [Fact]
    public async Task UpdateAsync_WhenRelationDoesNotExist_ShouldReturnRelationNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();
        var request = new UpdateRelationRequest(RelationType.Spouse, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Editor);

        _repoMock
            .Setup(r => r.GetByIdAsync(boardId, relationId))
            .ReturnsAsync((Relation?)null);

        var result = await _handler.UpdateAsync(boardId, relationId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Relations.RelationNotFound");
    }

    [Fact]
    public async Task UpdateAsync_WhenRelationExists_ShouldReturnUpdatedRelation()
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();
        var personAId = Guid.NewGuid();
        var personBId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var request = new UpdateRelationRequest(RelationType.Partner, null, null, "Getrennt", "Notiz");

        var existing = new Relation(relationId, boardId, personAId, personBId, RelationType.Spouse,
            createdAt, null, null, null, null);

        var updated = new Relation(relationId, boardId, personAId, personBId, RelationType.Partner,
            createdAt, null, null, "Getrennt", "Notiz");

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetByIdAsync(boardId, relationId))
            .ReturnsAsync(existing);

        _repoMock
            .Setup(r => r.UpdateAsync(boardId, relationId, request, null, null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(updated);

        var result = await _handler.UpdateAsync(boardId, relationId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(relationId);
        result.Value.Type.Should().Be(RelationType.Partner);
        result.Value.EndReason.Should().Be("Getrennt");
        result.Value.Notes.Should().Be("Notiz");
        result.Value.StartDate.Should().BeNull();
        result.Value.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenStartDateIsAdded_ShouldCreateFuzzyDateAndReturnItInResponse()
    {
        var boardId = Guid.NewGuid();
        var relationId = Guid.NewGuid();
        var personAId = Guid.NewGuid();
        var personBId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var startDateInput = new FuzzyDateRequest(FuzzyDatePrecision.Year, new DateOnly(2010, 1, 1), null, null, null, null);
        var request = new UpdateRelationRequest(RelationType.Spouse, startDateInput, null, null, null);

        var existing = new Relation(relationId, boardId, personAId, personBId, RelationType.Spouse,
            createdAt, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetByIdAsync(boardId, relationId))
            .ReturnsAsync(existing);

        _fuzzyDateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FuzzyDate>(), _connectionMock.Object, _transactionMock.Object))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.UpdateAsync(boardId, relationId, request, It.IsAny<Guid?>(), null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync((Guid bId, Guid rId, UpdateRelationRequest req, Guid? startDateId, Guid? endDateId, IDbConnection c, IDbTransaction t) =>
                new Relation(rId, bId, personAId, personBId, RelationType.Spouse,
                    createdAt, startDateId, null, null, null));

        var result = await _handler.UpdateAsync(boardId, relationId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.StartDate.Should().NotBeNull();
        result.Value.StartDate!.Precision.Should().Be(FuzzyDatePrecision.Year);
        result.Value.StartDate.Date.Should().Be(new DateOnly(2010, 1, 1));
        result.Value.EndDate.Should().BeNull();
    }
}
