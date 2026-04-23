using System.Data;
using FamilyTreeApiV2.Features.Persons;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Persons;

public class PersonsHandler_CreateTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - Only Owner and Editor may create (Viewer → Forbidden)

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IPersonsRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IDbConnection> _connectionMock = new();
    private readonly Mock<IDbTransaction> _transactionMock = new();
    private readonly PersonsHandler _handler;

    public PersonsHandler_CreateTests()
    {
        _connectionMock.Setup(c => c.BeginTransaction()).Returns(_transactionMock.Object);
        _connectionFactoryMock.Setup(f => f.CreateConnection()).Returns(_connectionMock.Object);

        _handler = new PersonsHandler(_repoMock.Object, _fuzzyDateRepoMock.Object, _connectionFactoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenCallerIsNotBoardMember_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var request = new CreatePersonRequest("Anna", "Müller", null, null, null, null, null, null, null, null, null, null, null);

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Persons.BoardNotFound");
    }

    [Fact]
    public async Task CreateAsync_WhenCallerIsViewer_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var request = new CreatePersonRequest("Anna", "Müller", null, null, null, null, null, null, null, null, null, null, null);

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Viewer);

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Persons.Forbidden");
    }

    [Theory]
    [InlineData(BoardRole.Owner)]
    [InlineData(BoardRole.Editor)]
    public async Task CreateAsync_WhenCallerIsOwnerOrEditor_ShouldReturnCreatedPerson(BoardRole role)
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var request = new CreatePersonRequest("Anna", "Müller", null, null, Gender.Female, null, null, null, null, null, null, null, null);

        var row = new Person(personId, boardId, "Anna", "Müller", null, null, Gender.Female,
            null, null, null, null, null, null, createdAt, null, null);

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(role);

        _repoMock
            .Setup(r => r.CreateAsync(boardId, request, null, null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(row);

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(personId);
        result.Value.FirstName.Should().Be("Anna");
        result.Value.Gender.Should().Be(Gender.Female);
        result.Value.BirthDate.Should().BeNull();
        result.Value.DeathDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIncludesBirthDate_ShouldCreateFuzzyDateAndReturnItInResponse()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var birthDateInput = new FuzzyDateRequest(FuzzyDatePrecision.Year, new DateOnly(1850, 1, 1), null, null, null, null);
        var request = new CreatePersonRequest("Anna", "Müller", null, null, null, null, birthDateInput, null, null, null, null, null, null);

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _fuzzyDateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FuzzyDate>(), _connectionMock.Object, _transactionMock.Object))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.CreateAsync(boardId, request, It.IsAny<Guid?>(), null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync((Guid bId, CreatePersonRequest req, Guid? birthDateId, Guid? deathDateId, IDbConnection c, IDbTransaction t) =>
                new Person(personId, bId, "Anna", "Müller", null, null, null,
                    null, null, null, null, null, null, createdAt, birthDateId, null));

        var result = await _handler.CreateAsync(boardId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.BirthDate.Should().NotBeNull();
        result.Value.BirthDate!.Precision.Should().Be(FuzzyDatePrecision.Year);
        result.Value.BirthDate.Date.Should().Be(new DateOnly(1850, 1, 1));
        result.Value.DeathDate.Should().BeNull();

        _fuzzyDateRepoMock.Verify(r => r.CreateAsync(
            It.Is<FuzzyDate>(d => d.Precision == FuzzyDatePrecision.Year && d.Date == new DateOnly(1850, 1, 1)),
            _connectionMock.Object, _transactionMock.Object), Times.Once);
    }
}
