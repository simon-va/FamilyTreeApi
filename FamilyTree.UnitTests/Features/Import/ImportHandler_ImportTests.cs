using System.Data;
using FamilyTreeApiV2.Features.Boards;
using FamilyTreeApiV2.Features.Import;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Persons;
using FamilyTreeApiV2.Features.Relations;
using FamilyTreeApiV2.Features.Residences;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;
using Moq;

namespace FamilyTree.UnitTests.Features.Import;

public class ImportHandler_ImportTests
{
    // Rules:
    // - Authentication via Tobit must succeed (null token → AuthenticationFailed)
    // - All three V1 API calls must succeed (null → OldApiFailed)
    // - Relations/Residences with unmapped V1 person IDs are silently skipped

    private static readonly Guid UserId = new Guid("00000000-0000-0000-0000-000000000001");

    private readonly Mock<ITobitAuthService> _authServiceMock = new();
    private readonly Mock<ITobitApiService> _apiServiceMock = new();
    private readonly Mock<IBoardsRepository> _boardsRepoMock = new();
    private readonly Mock<IMembersRepository> _membersRepoMock = new();
    private readonly Mock<IPersonsRepository> _personsRepoMock = new();
    private readonly Mock<IRelationsRepository> _relationsRepoMock = new();
    private readonly Mock<IResidencesRepository> _residencesRepoMock = new();
    private readonly Mock<IFuzzyDateRepository> _fuzzyDateRepoMock = new();
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock = new();
    private readonly Mock<IDbConnection> _connectionMock = new();
    private readonly Mock<IDbTransaction> _transactionMock = new();
    private readonly ImportHandler _handler;

    public ImportHandler_ImportTests()
    {
        _connectionMock.Setup(c => c.BeginTransaction()).Returns(_transactionMock.Object);
        _connectionFactoryMock.Setup(f => f.CreateConnection()).Returns(_connectionMock.Object);

        _handler = new ImportHandler(
            _authServiceMock.Object,
            _apiServiceMock.Object,
            _boardsRepoMock.Object,
            _membersRepoMock.Object,
            _personsRepoMock.Object,
            _relationsRepoMock.Object,
            _residencesRepoMock.Object,
            _fuzzyDateRepoMock.Object,
            _connectionFactoryMock.Object);
    }

    [Fact]
    public async Task ImportAsync_WhenAuthFails_ShouldReturnAuthenticationFailedError()
    {
        var request = new ImportRequest("user", "wrong-password");

        _authServiceMock
            .Setup(s => s.GetTokenAsync(request.Username, request.Password))
            .ReturnsAsync((string?)null);

        var result = await _handler.ImportAsync(request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Import.AuthenticationFailed");
    }

    [Fact]
    public async Task ImportAsync_WhenPersonsApiFails_ShouldReturnOldApiFailedError()
    {
        var request = new ImportRequest("user", "pass");

        _authServiceMock
            .Setup(s => s.GetTokenAsync(request.Username, request.Password))
            .ReturnsAsync("valid-token");

        _apiServiceMock
            .Setup(s => s.GetPersonsAsync("valid-token"))
            .ReturnsAsync((IEnumerable<V1Person>?)null);

        var result = await _handler.ImportAsync(request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Import.OldApiFailed");
    }

    [Fact]
    public async Task ImportAsync_WhenRelationsApiFails_ShouldReturnOldApiFailedError()
    {
        var request = new ImportRequest("user", "pass");

        _authServiceMock
            .Setup(s => s.GetTokenAsync(request.Username, request.Password))
            .ReturnsAsync("valid-token");

        _apiServiceMock
            .Setup(s => s.GetPersonsAsync("valid-token"))
            .ReturnsAsync(Array.Empty<V1Person>());

        _apiServiceMock
            .Setup(s => s.GetRelationsAsync("valid-token"))
            .ReturnsAsync((IEnumerable<V1Relation>?)null);

        var result = await _handler.ImportAsync(request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Import.OldApiFailed");
    }

    [Fact]
    public async Task ImportAsync_WhenResidencesApiFails_ShouldReturnOldApiFailedError()
    {
        var request = new ImportRequest("user", "pass");

        _authServiceMock
            .Setup(s => s.GetTokenAsync(request.Username, request.Password))
            .ReturnsAsync("valid-token");

        _apiServiceMock
            .Setup(s => s.GetPersonsAsync("valid-token"))
            .ReturnsAsync(Array.Empty<V1Person>());

        _apiServiceMock
            .Setup(s => s.GetRelationsAsync("valid-token"))
            .ReturnsAsync(Array.Empty<V1Relation>());

        _apiServiceMock
            .Setup(s => s.GetResidencesAsync("valid-token"))
            .ReturnsAsync((IEnumerable<V1Residence>?)null);

        var result = await _handler.ImportAsync(request, UserId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Import.OldApiFailed");
    }

    [Fact]
    public async Task ImportAsync_WhenDataIsEmpty_ShouldCreateBoardAndReturnResponse()
    {
        var boardId = Guid.NewGuid();
        var request = new ImportRequest("user", "pass");
        var board = new Board(boardId, "Importierter Stammbaum", BoardRole.Owner, DateTime.UtcNow);

        SetupSuccessfulAuth(request);
        SetupEmptyApiResponses();

        _boardsRepoMock
            .Setup(r => r.CreateBoardAsync("Importierter Stammbaum", _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(board);

        var result = await _handler.ImportAsync(request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.BoardId.Should().Be(boardId);
        result.Value.BoardName.Should().Be("Importierter Stammbaum");

        _membersRepoMock.Verify(r => r.AddOwnerAsync(boardId, UserId, _connectionMock.Object, _transactionMock.Object), Times.Once);
        _personsRepoMock.Verify(r => r.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreatePersonRequest>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_WhenPersonsExist_ShouldCreatePersonsAndReturnBoardId()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var request = new ImportRequest("user", "pass");
        var board = new Board(boardId, "Importierter Stammbaum", BoardRole.Owner, DateTime.UtcNow);

        var v1Person = new V1Person("v1-1", "Anna", "Müller", null, null, "female", null, null, null, null, null, null, null, null);

        SetupSuccessfulAuth(request);

        _apiServiceMock
            .Setup(s => s.GetPersonsAsync("valid-token"))
            .ReturnsAsync(new[] { v1Person });

        _apiServiceMock
            .Setup(s => s.GetRelationsAsync("valid-token"))
            .ReturnsAsync(Array.Empty<V1Relation>());

        _apiServiceMock
            .Setup(s => s.GetResidencesAsync("valid-token"))
            .ReturnsAsync(Array.Empty<V1Residence>());

        _boardsRepoMock
            .Setup(r => r.CreateBoardAsync(It.IsAny<string>(), _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(board);

        _personsRepoMock
            .Setup(r => r.CreateAsync(boardId, It.IsAny<CreatePersonRequest>(), null, null, _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(new Person(personId, boardId, "Anna", "Müller", null, null, Gender.Female, null, null, null, null, null, null, DateTime.UtcNow, null, null));

        var result = await _handler.ImportAsync(request, UserId);

        result.IsError.Should().BeFalse();
        result.Value.BoardId.Should().Be(boardId);

        _personsRepoMock.Verify(r => r.CreateAsync(boardId, It.Is<CreatePersonRequest>(p =>
            p.FirstName == "Anna" && p.LastName == "Müller" && p.Gender == Gender.Female),
            null, null, _connectionMock.Object, _transactionMock.Object), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WhenRelationPersonIdNotInMap_ShouldSkipRelationSilently()
    {
        var boardId = Guid.NewGuid();
        var request = new ImportRequest("user", "pass");
        var board = new Board(boardId, "Importierter Stammbaum", BoardRole.Owner, DateTime.UtcNow);

        var v1Relation = new V1Relation("rel-1", "unknown-person-a", "unknown-person-b",
            "spouse", null, null, null, "");

        SetupSuccessfulAuth(request);

        _apiServiceMock
            .Setup(s => s.GetPersonsAsync("valid-token"))
            .ReturnsAsync(Array.Empty<V1Person>());

        _apiServiceMock
            .Setup(s => s.GetRelationsAsync("valid-token"))
            .ReturnsAsync(new[] { v1Relation });

        _apiServiceMock
            .Setup(s => s.GetResidencesAsync("valid-token"))
            .ReturnsAsync(Array.Empty<V1Residence>());

        _boardsRepoMock
            .Setup(r => r.CreateBoardAsync(It.IsAny<string>(), _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(board);

        var result = await _handler.ImportAsync(request, UserId);

        result.IsError.Should().BeFalse();
        _relationsRepoMock.Verify(r => r.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateRelationRequest>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_WhenResidencePersonIdNotInMap_ShouldSkipResidenceSilently()
    {
        var boardId = Guid.NewGuid();
        var request = new ImportRequest("user", "pass");
        var board = new Board(boardId, "Importierter Stammbaum", BoardRole.Owner, DateTime.UtcNow);

        var v1Residence = new V1Residence("res-1", "unknown-person", "Berlin", null, null, null, null, null, null, null, null);

        SetupSuccessfulAuth(request);

        _apiServiceMock
            .Setup(s => s.GetPersonsAsync("valid-token"))
            .ReturnsAsync(Array.Empty<V1Person>());

        _apiServiceMock
            .Setup(s => s.GetRelationsAsync("valid-token"))
            .ReturnsAsync(Array.Empty<V1Relation>());

        _apiServiceMock
            .Setup(s => s.GetResidencesAsync("valid-token"))
            .ReturnsAsync(new[] { v1Residence });

        _boardsRepoMock
            .Setup(r => r.CreateBoardAsync(It.IsAny<string>(), _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(board);

        var result = await _handler.ImportAsync(request, UserId);

        result.IsError.Should().BeFalse();
        _residencesRepoMock.Verify(r => r.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateResidenceRequest>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_WhenMovedToResidenceIdSet_ShouldCallSetMovedToResidenceId()
    {
        var boardId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var residenceAId = Guid.NewGuid();
        var residenceBId = Guid.NewGuid();
        var request = new ImportRequest("user", "pass");
        var board = new Board(boardId, "Importierter Stammbaum", BoardRole.Owner, DateTime.UtcNow);

        var v1Person = new V1Person("p-1", "Anna", "Müller", null, null, null, null, null, null, null, null, null, null, null);
        var v1ResidenceA = new V1Residence("res-a", "p-1", "Berlin", null, null, null, null, null, "res-b", null, null);
        var v1ResidenceB = new V1Residence("res-b", "p-1", "Hamburg", null, null, null, null, null, null, null, null);

        SetupSuccessfulAuth(request);

        _apiServiceMock
            .Setup(s => s.GetPersonsAsync("valid-token"))
            .ReturnsAsync(new[] { v1Person });

        _apiServiceMock
            .Setup(s => s.GetRelationsAsync("valid-token"))
            .ReturnsAsync(Array.Empty<V1Relation>());

        _apiServiceMock
            .Setup(s => s.GetResidencesAsync("valid-token"))
            .ReturnsAsync(new[] { v1ResidenceA, v1ResidenceB });

        _boardsRepoMock
            .Setup(r => r.CreateBoardAsync(It.IsAny<string>(), _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(board);

        _personsRepoMock
            .Setup(r => r.CreateAsync(boardId, It.IsAny<CreatePersonRequest>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(new Person(personId, boardId, "Anna", "Müller", null, null, null, null, null, null, null, null, null, DateTime.UtcNow, null, null));

        _residencesRepoMock
            .SetupSequence(r => r.CreateAsync(boardId, It.IsAny<CreateResidenceRequest>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), _connectionMock.Object, _transactionMock.Object))
            .ReturnsAsync(new Residence(residenceAId, boardId, personId, "Berlin", null, null, null, null, null, null, null, null, DateTime.UtcNow))
            .ReturnsAsync(new Residence(residenceBId, boardId, personId, "Hamburg", null, null, null, null, null, null, null, null, DateTime.UtcNow));

        var result = await _handler.ImportAsync(request, UserId);

        result.IsError.Should().BeFalse();

        _residencesRepoMock.Verify(r => r.SetMovedToResidenceIdAsync(
            boardId, residenceAId, residenceBId, _connectionMock.Object, _transactionMock.Object), Times.Once);
    }

    private void SetupSuccessfulAuth(ImportRequest request)
    {
        _authServiceMock
            .Setup(s => s.GetTokenAsync(request.Username, request.Password))
            .ReturnsAsync("valid-token");
    }

    private void SetupEmptyApiResponses()
    {
        _apiServiceMock.Setup(s => s.GetPersonsAsync("valid-token")).ReturnsAsync(Array.Empty<V1Person>());
        _apiServiceMock.Setup(s => s.GetRelationsAsync("valid-token")).ReturnsAsync(Array.Empty<V1Relation>());
        _apiServiceMock.Setup(s => s.GetResidencesAsync("valid-token")).ReturnsAsync(Array.Empty<V1Residence>());
    }
}
