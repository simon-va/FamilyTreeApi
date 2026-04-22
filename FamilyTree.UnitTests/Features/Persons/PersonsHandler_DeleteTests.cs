using FamilyTreeApiV2.Features.Persons;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Persons;

public class PersonsHandler_DeleteTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - Only Owner and Editor may delete (Viewer → Forbidden)
    // - Person must exist (false from repository → PersonNotFound)

    private readonly Mock<IPersonsRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly PersonsHandler _handler;

    public PersonsHandler_DeleteTests()
    {
        _handler = new PersonsHandler(_repoMock.Object, _fuzzyDateRepoMock.Object, _connectionFactoryMock.Object);
    }

    [Fact]
    public async Task DeleteAsync_WhenCallerIsNotBoardMember_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, "user-1"))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.DeleteAsync(boardId, personId, "user-1");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Persons.BoardNotFound");
    }

    [Fact]
    public async Task DeleteAsync_WhenCallerIsViewer_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, "user-1"))
            .ReturnsAsync(BoardRole.Viewer);

        var result = await _handler.DeleteAsync(boardId, personId, "user-1");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Persons.Forbidden");
    }

    [Fact]
    public async Task DeleteAsync_WhenPersonDoesNotExist_ShouldReturnPersonNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, "user-1"))
            .ReturnsAsync(BoardRole.Editor);

        _repoMock
            .Setup(r => r.DeleteAsync(boardId, personId))
            .ReturnsAsync(false);

        var result = await _handler.DeleteAsync(boardId, personId, "user-1");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Persons.PersonNotFound");
    }

    [Theory]
    [InlineData(BoardRole.Owner)]
    [InlineData(BoardRole.Editor)]
    public async Task DeleteAsync_WhenCallerIsOwnerOrEditor_ShouldReturnDeleted(BoardRole role)
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, "user-1"))
            .ReturnsAsync(role);

        _repoMock
            .Setup(r => r.DeleteAsync(boardId, personId))
            .ReturnsAsync(true);

        var result = await _handler.DeleteAsync(boardId, personId, "user-1");

        result.IsError.Should().BeFalse();
    }
}
