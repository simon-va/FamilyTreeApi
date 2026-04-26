using System.Data;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Residences;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Residences;

public class ResidencesHandler_CreateTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - Only Owner and Editor may create (Viewer → Forbidden)
    // - Person must belong to the board (false → PersonNotFound)

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IResidencesRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IDbConnection> _connectionMock = new();
    private readonly Mock<IDbTransaction> _transactionMock = new();
    private readonly Mock<IMembersRepository> _memberRepoMock = new();
    private readonly ResidencesHandler _handler;

    public ResidencesHandler_CreateTests()
    {
        _connectionMock.Setup(c => c.BeginTransaction()).Returns(_transactionMock.Object);
        _connectionFactoryMock.Setup(f => f.CreateConnection()).Returns(_connectionMock.Object);

        _handler = new ResidencesHandler(_repoMock.Object, _fuzzyDateRepoMock.Object, _connectionFactoryMock.Object, _memberRepoMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenCallerIsNotBoardMember_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var request = new CreateResidenceRequest(Guid.NewGuid(), null, null, null, null, null, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Residences.BoardNotFound");
    }

    [Fact]
    public async Task CreateAsync_WhenCallerIsViewer_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var request = new CreateResidenceRequest(Guid.NewGuid(), null, null, null, null, null, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Viewer);

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Residences.Forbidden");
    }

    [Fact]
    public async Task CreateAsync_WhenPersonDoesNotBelongToBoard_ShouldReturnPersonNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var request = new CreateResidenceRequest(personId, null, null, null, null, null, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.DoesPersonBelongToBoardAsync(boardId, personId))
            .ReturnsAsync(false);

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Residences.PersonNotFound");
    }

    [Theory]
    [InlineData(BoardRole.Owner)]
    [InlineData(BoardRole.Editor)]
    public async Task CreateAsync_WhenCallerIsOwnerOrEditor_ShouldReturnCreatedResidence(BoardRole role)
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var request = new CreateResidenceRequest(personId, "München", "Deutschland", null, null, null, null, null, null, null);

        var residence = new Residence(residenceId, boardId, personId, "München", "Deutschland",
            null, null, null, null, null, null, null, createdAt);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(role);

        _repoMock
            .Setup(r => r.DoesPersonBelongToBoardAsync(boardId, personId))
            .ReturnsAsync(true);

        _repoMock
            .Setup(r => r.CreateAsync(boardId, request, null, null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(residence);

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(residenceId);
        result.Value.City.Should().Be("München");
        result.Value.Country.Should().Be("Deutschland");
        result.Value.StartDateId.Should().BeNull();
        result.Value.EndDateId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIncludesStartDate_ShouldCreateFuzzyDateAndReturnIdInResponse()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var startDateInput = new FuzzyDateRequest(FuzzyDatePrecision.Year, new DateOnly(1920, 1, 1), null, null, null, null);
        var request = new CreateResidenceRequest(personId, "Berlin", null, null, null, null, null, null, startDateInput, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.DoesPersonBelongToBoardAsync(boardId, personId))
            .ReturnsAsync(true);

        _fuzzyDateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FuzzyDate>(), _connectionMock.Object, _transactionMock.Object))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.CreateAsync(boardId, request, It.IsAny<Guid?>(), null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync((Guid bId, CreateResidenceRequest req, Guid? startDateId, Guid? endDateId, IDbConnection c, IDbTransaction t) =>
                new Residence(residenceId, bId, personId, "Berlin", null, null, null, null, null, null, startDateId, null, createdAt));

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.StartDateId.Should().NotBeNull();
        result.Value.EndDateId.Should().BeNull();

        _fuzzyDateRepoMock.Verify(r => r.CreateAsync(
            It.Is<FuzzyDate>(d => d.Precision == FuzzyDatePrecision.Year && d.Date == new DateOnly(1920, 1, 1)),
            _connectionMock.Object, _transactionMock.Object), Times.Once);
    }
}
