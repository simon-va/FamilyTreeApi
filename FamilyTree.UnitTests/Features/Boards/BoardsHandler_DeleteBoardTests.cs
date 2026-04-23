using ErrorOr;
using FamilyTreeApiV2.Features.Boards;
using FamilyTreeApiV2.Shared;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Boards;

public class BoardsHandler_DeleteBoardTests
{
    // Rules:
    // - Board must exist and caller must be a member (role is null → NotFound)
    // - Only the board owner can delete it (role != Owner → Forbidden)

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IBoardsRepository> _repoMock = new();
    private readonly BoardsHandler _handler;

    public BoardsHandler_DeleteBoardTests()
    {
        _handler = new BoardsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task DeleteBoardAsync_WhenBoardNotFound_ShouldReturnNotFoundError()
    {
        var boardId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetUserRoleOnBoardAsync(boardId, UserId))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.DeleteBoardAsync(boardId, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Boards.NotFound");
    }

    [Fact]
    public async Task DeleteBoardAsync_WhenCallerIsEditor_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetUserRoleOnBoardAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Editor);

        var result = await _handler.DeleteBoardAsync(boardId, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Boards.Forbidden");
    }

    [Fact]
    public async Task DeleteBoardAsync_WhenCallerIsOwner_ShouldDeleteAndReturnDeleted()
    {
        var boardId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetUserRoleOnBoardAsync(boardId, UserId))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.DeleteBoardAsync(boardId))
            .Returns(Task.CompletedTask);

        var result = await _handler.DeleteBoardAsync(boardId, UserId);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        _repoMock.Verify(r => r.DeleteBoardAsync(boardId), Times.Once);
    }
}
