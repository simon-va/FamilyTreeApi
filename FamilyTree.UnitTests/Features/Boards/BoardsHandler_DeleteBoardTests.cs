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
            .Setup(r => r.GetUserRoleOnBoardAsync(boardId, "user-1"))
            .ReturnsAsync((BoardRole?)null);

        var result = await _handler.DeleteBoardAsync(boardId, "user-1");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Boards.NotFound");
    }

    [Fact]
    public async Task DeleteBoardAsync_WhenCallerIsEditor_ShouldReturnForbiddenError()
    {
        var boardId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetUserRoleOnBoardAsync(boardId, "user-1"))
            .ReturnsAsync(BoardRole.Editor);

        var result = await _handler.DeleteBoardAsync(boardId, "user-1");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Boards.Forbidden");
    }

    [Fact]
    public async Task DeleteBoardAsync_WhenCallerIsOwner_ShouldSoftDeleteAndReturnDeleted()
    {
        var boardId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetUserRoleOnBoardAsync(boardId, "user-1"))
            .ReturnsAsync(BoardRole.Owner);

        _repoMock
            .Setup(r => r.SoftDeleteBoardAsync(boardId))
            .Returns(Task.CompletedTask);

        var result = await _handler.DeleteBoardAsync(boardId, "user-1");

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        _repoMock.Verify(r => r.SoftDeleteBoardAsync(boardId), Times.Once);
    }
}
