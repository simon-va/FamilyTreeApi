using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Residences;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Residences;

public class ResidencesHandler_GetAllTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - All members (Owner, Editor, Viewer) may read

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IResidencesRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IMembersRepository> _memberRepoMock = new();
    private readonly ResidencesHandler _handler;

    public ResidencesHandler_GetAllTests()
    {
        _handler = new ResidencesHandler(_repoMock.Object, _fuzzyDateRepoMock.Object, _connectionFactoryMock.Object, _memberRepoMock.Object);
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
        result.FirstError.Code.Should().Be("Residences.BoardNotFound");
    }

    [Theory]
    [InlineData(BoardRole.Owner)]
    [InlineData(BoardRole.Editor)]
    [InlineData(BoardRole.Viewer)]
    public async Task GetAllAsync_WhenCallerIsMember_ShouldReturnMappedResidenceList(BoardRole role)
    {
        var boardId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var residence = new Residence(residenceId, boardId, personId, "Berlin", "Deutschland",
            null, null, null, null, null, null, null, createdAt);

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(role);

        _repoMock
            .Setup(r => r.GetAllAsync(boardId))
            .ReturnsAsync(new[] { residence });

        var result = await _handler.GetAllAsync(boardId, UserId);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(residenceId);
        result.Value[0].City.Should().Be("Berlin");
        result.Value[0].Country.Should().Be("Deutschland");
        result.Value[0].StartDateId.Should().BeNull();
        result.Value[0].EndDateId.Should().BeNull();
    }
}
