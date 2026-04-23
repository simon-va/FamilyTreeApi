using FamilyTreeApiV2.Features.Boards;
using FamilyTreeApiV2.Shared;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Boards;

public class BoardsHandler_CreateBoardTests
{
    // Rules:
    // - No business rules; CreateBoardAsync delegates entirely to the repository and maps the result.

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IBoardsRepository> _repoMock = new();
    private readonly BoardsHandler _handler;

    public BoardsHandler_CreateBoardTests()
    {
        _handler = new BoardsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task CreateBoardAsync_WhenRepositoryReturnsBoard_ShouldReturnMappedBoardResponse()
    {
        var boardId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var row = new Board(boardId, "My Family Tree", BoardRole.Owner, createdAt);

        _repoMock
            .Setup(r => r.CreateBoardAsync("My Family Tree", UserId))
            .ReturnsAsync(row);

        var result = await _handler.CreateBoardAsync(new CreateBoardRequest("My Family Tree"), UserId);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(boardId);
        result.Value.Name.Should().Be("My Family Tree");
        result.Value.Role.Should().Be(BoardRole.Owner);
        result.Value.CreatedAt.Should().Be(createdAt);
    }
}
