using System.Data;
using FamilyTreeApiV2.Features.Boards;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Boards;

public class BoardsHandler_CreateBoardTests
{
    // Rules:
    // - No business rules; CreateBoardAsync orchestrates board + owner creation and maps the result.

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<IBoardsRepository> _repoMock = new();
    private readonly Mock<IMembersRepository> _memberRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IDbConnection> _connectionMock = new();
    private readonly Mock<IDbTransaction> _transactionMock = new();
    private readonly BoardsHandler _handler;

    public BoardsHandler_CreateBoardTests()
    {
        _connectionFactoryMock.Setup(f => f.CreateConnection()).Returns(_connectionMock.Object);
        _connectionMock.Setup(c => c.BeginTransaction()).Returns(_transactionMock.Object);

        _handler = new BoardsHandler(_repoMock.Object, _memberRepoMock.Object, _connectionFactoryMock.Object);
    }

    [Fact]
    public async Task CreateBoardAsync_WhenRepositoriesSucceed_ShouldReturnMappedBoardResponse()
    {
        var boardId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var board = new Board(boardId, "My Family Tree", BoardRole.Owner, createdAt);

        _repoMock
            .Setup(r => r.CreateBoardAsync("My Family Tree", _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(board);

        _memberRepoMock
            .Setup(r => r.AddOwnerAsync(boardId, UserId, _connectionMock.Object, _transactionMock.Object))
            .Returns(Task.CompletedTask);

        var result = await _handler.CreateBoardAsync(new CreateBoardRequest("My Family Tree"), UserId);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(boardId);
        result.Value.Name.Should().Be("My Family Tree");
        result.Value.Role.Should().Be(BoardRole.Owner);
        result.Value.CreatedAt.Should().Be(createdAt);
    }
}
