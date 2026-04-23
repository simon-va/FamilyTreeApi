using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Boards;

public record CreateBoardRequest(string Name);
public record Board(Guid Id, string Name, BoardRole Role, DateTime CreatedAt);
public record BoardResponse(Guid Id, string Name, BoardRole Role, DateTime CreatedAt);
