using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Persons;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Persons;

public class PersonsHandler_GetAllTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - All members (Owner, Editor, Viewer) may read

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IPersonsRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IMembersRepository> _memberRepoMock = new();
    private readonly PersonsHandler _handler;

    public PersonsHandler_GetAllTests()
    {
        _handler = new PersonsHandler(_repoMock.Object, _fuzzyDateRepoMock.Object, _connectionFactoryMock.Object, _memberRepoMock.Object);
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
        result.FirstError.Code.Should().Be("Persons.BoardNotFound");
    }

    [Theory]
    [InlineData(BoardRole.Owner)]
    [InlineData(BoardRole.Editor)]
    [InlineData(BoardRole.Viewer)]
    public async Task GetAllAsync_WhenCallerIsMember_ShouldReturnMappedPersonList(BoardRole role)
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var row = new Person(personId, boardId, "Anna", "Müller", null, null, null,
            null, null, null, null, null, null, createdAt, null, null);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(role);

        if (role == BoardRole.Viewer)
            _memberRepoMock
                .Setup(r => r.GetCallerPrivacyModeAsync(boardId, UserId))
                .ReturnsAsync(ViewerPrivacyMode.Full);

        _repoMock
            .Setup(r => r.GetAllAsync(boardId))
            .ReturnsAsync(new[] { (row, (FuzzyDate?)null, (FuzzyDate?)null) });

        var result = await _handler.GetAllAsync(boardId, UserId);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(personId);
        result.Value[0].FirstName.Should().Be("Anna");
        result.Value[0].LastName.Should().Be("Müller");
        result.Value[0].BirthDate.Should().BeNull();
        result.Value[0].DeathDate.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenPersonHasBirthDate_ShouldReturnBirthDateInResponse()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var dateId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var row = new Person(personId, boardId, "Anna", "Müller", null, null, null,
            null, null, null, null, null, null, createdAt, dateId, null);

        var birthDate = new FuzzyDate(dateId, FuzzyDatePrecision.Year, new DateOnly(1850, 1, 1), null, null, null, null, createdAt);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.GetAllAsync(boardId))
            .ReturnsAsync(new[] { (row, (FuzzyDate?)birthDate, (FuzzyDate?)null) });

        var result = await _handler.GetAllAsync(boardId, UserId);

        result.IsError.Should().BeFalse();
        var birthDate2 = result.Value[0].BirthDate;
        birthDate2.Should().NotBeNull();
        birthDate2!.Precision.Should().Be(FuzzyDatePrecision.Year);
        birthDate2.Date.Should().Be(new DateOnly(1850, 1, 1));
        result.Value[0].DeathDate.Should().BeNull();
    }
}
