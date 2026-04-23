using FamilyTreeApiV2.Features.Boards;
using FamilyTreeApiV2.Shared;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Boards;

public class BoardsHandler_GetBoardsTests
{
    // Rules:
    // - No business rules; GetBoardsAsync delegates entirely to the repository and maps the result.

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IBoardsRepository> _repoMock = new();
    private readonly BoardsHandler _handler;

    public BoardsHandler_GetBoardsTests()
    {
        _handler = new BoardsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task GetBoardsAsync_WhenRepositoryReturnsBoards_ShouldReturnMappedList()
    {
        var rows = new[]
        {
            new BoardRow(Guid.NewGuid(), "Board A", BoardRole.Owner, DateTime.UtcNow),
            new BoardRow(Guid.NewGuid(), "Board B", BoardRole.Editor, DateTime.UtcNow)
        };

        _repoMock
            .Setup(r => r.GetBoardsByUserIdAsync(UserId))
            .ReturnsAsync(rows);

        var result = await _handler.GetBoardsAsync(UserId);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
        result.Value[0].Name.Should().Be("Board A");
        result.Value[0].Role.Should().Be(BoardRole.Owner);
        result.Value[1].Name.Should().Be("Board B");
        result.Value[1].Role.Should().Be(BoardRole.Editor);
    }

    [Fact]
    public async Task GetBoardsAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
    {
        _repoMock
            .Setup(r => r.GetBoardsByUserIdAsync(UserId))
            .ReturnsAsync([]);

        var result = await _handler.GetBoardsAsync(UserId);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }
}
