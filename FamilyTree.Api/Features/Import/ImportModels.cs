namespace FamilyTreeApiV2.Features.Import;

public record ImportRequest(string Username, string Password);
public record ImportResponse(Guid BoardId, string BoardName);
