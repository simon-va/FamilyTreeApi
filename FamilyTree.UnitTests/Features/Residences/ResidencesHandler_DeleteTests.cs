using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Residences;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Residences;

public class ResidencesHandler_DeleteTests
{
    // Rules:
    // - Caller must be a member of the board (role is null → BoardNotFound)
    // - Only Owner and Editor may delete (Viewer → Forbidden)
    // - Residence must exist (false from repository → ResidenceNotFound)

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IResidencesRepository> _repoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IMembersRepository> _memberRepoMock = new();
    private readonly ResidencesHandler _handler;

    public ResidencesHandler_DeleteTests()
    {
        _handler = new ResidencesHandler(_repoMock.Object, _fuzzyDateRepoMock.Object, _connectionFactoryMock.Object, _memberRepoMock.Object);
    }

    [Fact]
    public async Task DeleteAsync_WhenCallerIsNotBoardMember_ShouldReturnBoardNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.DeleteAsync(boardId, residenceId, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Residences.BoardNotFound");
    }

    [Fact]
    public async Task DeleteAsync_WhenCallerIsViewer_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Viewer);

        var result = await _handler.DeleteAsync(boardId, residenceId, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Residences.Forbidden");
    }

    [Fact]
    public async Task DeleteAsync_WhenResidenceDoesNotExist_ShouldReturnResidenceNotFoundError()
    {
        var boardId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Editor);

        _repoMock
            .Setup(r => r.DeleteAsync(boardId, residenceId))
            .ReturnsAsync(false);

        var result = await _handler.DeleteAsync(boardId, residenceId, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Residences.ResidenceNotFound");
    }

    [Theory]
    [InlineData(BoardRole.Owner)]
    [InlineData(BoardRole.Editor)]
    public async Task DeleteAsync_WhenCallerIsOwnerOrEditor_ShouldReturnDeleted(BoardRole role)
    {
        var boardId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.GetCallerRoleAsync(boardId, UserId))
            .ReturnsAsync(role);

        _repoMock
            .Setup(r => r.DeleteAsync(boardId, residenceId))
            .ReturnsAsync(true);

        var result = await _handler.DeleteAsync(boardId, residenceId, UserId);

        result.IsError.Should().BeFalse();
    }
}
