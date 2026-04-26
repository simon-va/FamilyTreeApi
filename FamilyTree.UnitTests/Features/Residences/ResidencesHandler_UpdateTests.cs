using System.Data;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Residences;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Residences;

public class ResidencesHandler_UpdateTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - Only Owner and Editor may update (Viewer → Forbidden)
    // - Residence must exist (null from GetByIdAsync → ResidenceNotFound)

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IResidencesRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IDbConnection> _connectionMock = new();
    private readonly Mock<IDbTransaction> _transactionMock = new();
    private readonly Mock<IMembersRepository> _memberRepoMock = new();
    private readonly ResidencesHandler _handler;

    public ResidencesHandler_UpdateTests()
    {
        _connectionMock.Setup(c => c.BeginTransaction()).Returns(_transactionMock.Object);
        _connectionFactoryMock.Setup(f => f.CreateConnection()).Returns(_connectionMock.Object);

        _handler = new ResidencesHandler(_repoMock.Object, _fuzzyDateRepoMock.Object, _connectionFactoryMock.Object, _memberRepoMock.Object);
    }

    [Fact]
    public async Task UpdateAsync_WhenCallerIsNotBoardMember_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();
        var request = new UpdateResidenceRequest(null, null, null, null, null, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.UpdateAsync(boardId, residenceId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Residences.BoardNotFound");
    }

    [Fact]
    public async Task UpdateAsync_WhenCallerIsViewer_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();
        var request = new UpdateResidenceRequest(null, null, null, null, null, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Viewer);

        var result = await _handler.UpdateAsync(boardId, residenceId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Residences.Forbidden");
    }

    [Fact]
    public async Task UpdateAsync_WhenResidenceDoesNotExist_ShouldReturnResidenceNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();
        var request = new UpdateResidenceRequest("Hamburg", null, null, null, null, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Editor);

        _repoMock
            .Setup(r => r.GetByIdAsync(boardId, residenceId))
            .ReturnsAsync((Residence?)null);

        var result = await _handler.UpdateAsync(boardId, residenceId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Residences.ResidenceNotFound");
    }

    [Fact]
    public async Task UpdateAsync_WhenResidenceExists_ShouldReturnUpdatedResidence()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var request = new UpdateResidenceRequest("Hamburg", "Deutschland", null, "Notiz", null, null, null, null, null);

        var existing = new Residence(residenceId, boardId, personId, "Berlin", "Deutschland",
            null, null, null, null, null, null, null, createdAt);

        var updated = new Residence(residenceId, boardId, personId, "Hamburg", "Deutschland",
            null, "Notiz", null, null, null, null, null, createdAt);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetByIdAsync(boardId, residenceId))
            .ReturnsAsync(existing);

        _repoMock
            .Setup(r => r.UpdateAsync(boardId, residenceId, request, null, null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(updated);

        var result = await _handler.UpdateAsync(boardId, residenceId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(residenceId);
        result.Value.City.Should().Be("Hamburg");
        result.Value.Notes.Should().Be("Notiz");
        result.Value.StartDateId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenStartDateIsAdded_ShouldCreateFuzzyDateAndReturnIdInResponse()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var startDateInput = new FuzzyDateRequest(FuzzyDatePrecision.Year, new DateOnly(1950, 1, 1), null, null, null, null);
        var request = new UpdateResidenceRequest("Berlin", null, null, null, null, null, null, startDateInput, null);

        var existing = new Residence(residenceId, boardId, personId, "Berlin", null,
            null, null, null, null, null, null, null, createdAt);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetByIdAsync(boardId, residenceId))
            .ReturnsAsync(existing);

        _fuzzyDateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FuzzyDate>(), _connectionMock.Object, _transactionMock.Object))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.UpdateAsync(boardId, residenceId, request, It.IsAny<Guid?>(), null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync((Guid bId, Guid rId, UpdateResidenceRequest req, Guid? startDateId, Guid? endDateId, IDbConnection c, IDbTransaction t) =>
                new Residence(rId, bId, personId, "Berlin", null, null, null, null, null, null, startDateId, null, createdAt));

        var result = await _handler.UpdateAsync(boardId, residenceId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.StartDateId.Should().NotBeNull();
        result.Value.EndDateId.Should().BeNull();

        _fuzzyDateRepoMock.Verify(r => r.CreateAsync(
            It.Is<FuzzyDate>(d => d.Precision == FuzzyDatePrecision.Year && d.Date == new DateOnly(1950, 1, 1)),
            _connectionMock.Object, _transactionMock.Object), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenStartDateIsRemoved_ShouldDeleteFuzzyDateAndReturnNullIdInResponse()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();
        var existingDateId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var request = new UpdateResidenceRequest("Berlin", null, null, null, null, null, null, null, null);

        var existing = new Residence(residenceId, boardId, personId, "Berlin", null,
            null, null, null, null, null, existingDateId, null, createdAt);

        var updated = new Residence(residenceId, boardId, personId, "Berlin", null,
            null, null, null, null, null, null, null, createdAt);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetByIdAsync(boardId, residenceId))
            .ReturnsAsync(existing);

        _fuzzyDateRepoMock
            .Setup(r => r.DeleteAsync(existingDateId, _connectionMock.Object, _transactionMock.Object))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.UpdateAsync(boardId, residenceId, request, null, null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(updated);

        var result = await _handler.UpdateAsync(boardId, residenceId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.StartDateId.Should().BeNull();

        _fuzzyDateRepoMock.Verify(r => r.DeleteAsync(existingDateId, _connectionMock.Object, _transactionMock.Object), Times.Once);
    }
}
