using System.Data;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Persons;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Persons;

public class PersonsHandler_UpdateTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - Only Owner and Editor may update (Viewer → Forbidden)
    // - Person must exist (null from GetByIdAsync → PersonNotFound)

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IPersonsRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IDbConnection> _connectionMock = new();
    private readonly Mock<IDbTransaction> _transactionMock = new();
    private readonly Mock<IMembersRepository> _memberRepoMock = new();
    private readonly PersonsHandler _handler;

    public PersonsHandler_UpdateTests()
    {
        _connectionMock.Setup(c => c.BeginTransaction()).Returns(_transactionMock.Object);
        _connectionFactoryMock.Setup(f => f.CreateConnection()).Returns(_connectionMock.Object);

        _handler = new PersonsHandler(_repoMock.Object, _fuzzyDateRepoMock.Object, _connectionFactoryMock.Object, _memberRepoMock.Object);
    }

    [Fact]
    public async Task UpdateAsync_WhenCallerIsNotBoardMember_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var request = new UpdatePersonRequest("Anna", "Müller", null, null, null, null, null, null, null, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.UpdateAsync(boardId, personId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Persons.BoardNotFound");
    }

    [Fact]
    public async Task UpdateAsync_WhenCallerIsViewer_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var request = new UpdatePersonRequest("Anna", "Müller", null, null, null, null, null, null, null, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Viewer);

        var result = await _handler.UpdateAsync(boardId, personId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Persons.Forbidden");
    }

    [Fact]
    public async Task UpdateAsync_WhenPersonDoesNotExist_ShouldReturnPersonNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var request = new UpdatePersonRequest("Anna", "Müller", null, null, null, null, null, null, null, null, null, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Editor);

        _repoMock
            .Setup(r => r.GetByIdAsync(boardId, personId))
            .ReturnsAsync((Person?)null);

        var result = await _handler.UpdateAsync(boardId, personId, request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Persons.PersonNotFound");
    }

    [Fact]
    public async Task UpdateAsync_WhenPersonExists_ShouldReturnUpdatedPerson()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var request = new UpdatePersonRequest("Anna", "Schmidt", null, "Müller", Gender.Female, null, null, null, null, null, null, null, "Notiz");

        var existing = new Person(personId, boardId, "Anna", "Müller", null, null, Gender.Female,
            null, null, null, null, null, null, createdAt, null, null);

        var updated = new Person(personId, boardId, "Anna", "Schmidt", null, "Müller", Gender.Female,
            null, null, null, null, null, "Notiz", createdAt, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetByIdAsync(boardId, personId))
            .ReturnsAsync(existing);

        _repoMock
            .Setup(r => r.UpdateAsync(boardId, personId, request, null, null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(updated);

        var result = await _handler.UpdateAsync(boardId, personId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(personId);
        result.Value.LastName.Should().Be("Schmidt");
        result.Value.BirthName.Should().Be("Müller");
        result.Value.Notes.Should().Be("Notiz");
        result.Value.BirthDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenBirthDateIsAdded_ShouldCreateFuzzyDateAndReturnItInResponse()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var birthDateInput = new FuzzyDateRequest(FuzzyDatePrecision.Year, new DateOnly(1850, 1, 1), null, null, null, null);
        var request = new UpdatePersonRequest("Anna", "Müller", null, null, null, null, birthDateInput, null, null, null, null, null, null);

        var existing = new Person(personId, boardId, "Anna", "Müller", null, null, null,
            null, null, null, null, null, null, createdAt, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetByIdAsync(boardId, personId))
            .ReturnsAsync(existing);

        _fuzzyDateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FuzzyDate>(), _connectionMock.Object, _transactionMock.Object))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.UpdateAsync(boardId, personId, request, It.IsAny<Guid?>(), null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync((Guid bId, Guid pId, UpdatePersonRequest req, Guid? birthDateId, Guid? deathDateId, IDbConnection c, IDbTransaction t) =>
                new Person(pId, bId, "Anna", "Müller", null, null, null,
                    null, null, null, null, null, null, createdAt, birthDateId, null));

        var result = await _handler.UpdateAsync(boardId, personId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.BirthDate.Should().NotBeNull();
        result.Value.BirthDate!.Precision.Should().Be(FuzzyDatePrecision.Year);
    }

    [Fact]
    public async Task UpdateAsync_WhenBirthDateIsRemoved_ShouldDeleteFuzzyDateAndReturnNullInResponse()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var existingDateId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var request = new UpdatePersonRequest("Anna", "Müller", null, null, null, null, null, null, null, null, null, null, null);

        var existing = new Person(personId, boardId, "Anna", "Müller", null, null, null,
            null, null, null, null, null, null, createdAt, existingDateId, null);

        var updated = new Person(personId, boardId, "Anna", "Müller", null, null, null,
            null, null, null, null, null, null, createdAt, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetByIdAsync(boardId, personId))
            .ReturnsAsync(existing);

        _fuzzyDateRepoMock
            .Setup(r => r.DeleteAsync(existingDateId, _connectionMock.Object, _transactionMock.Object))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.UpdateAsync(boardId, personId, request, null, null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(updated);

        var result = await _handler.UpdateAsync(boardId, personId, request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.BirthDate.Should().BeNull();

        _fuzzyDateRepoMock.Verify(r => r.DeleteAsync(existingDateId, _connectionMock.Object, _transactionMock.Object), Times.Once);
    }
}
