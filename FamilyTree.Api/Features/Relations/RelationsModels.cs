using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Relations;

public record CreateRelationRequest(
    Guid PersonAId,
    Guid PersonBId,
    RelationType Type,
    FuzzyDateRequest? StartDate,
    FuzzyDateRequest? EndDate,
    string? EndReason,
    string? Notes);

public record UpdateRelationRequest(
    RelationType Type,
    FuzzyDateRequest? StartDate,
    FuzzyDateRequest? EndDate,
    string? EndReason,
    string? Notes);

public record Relation(
    Guid Id,
    Guid BoardId,
    Guid PersonAId,
    Guid PersonBId,
    RelationType Type,
    DateTime CreatedAt,
    Guid? StartDateId,
    Guid? EndDateId,
    string? EndReason,
    string? Notes);

public record RelationResponse(
    Guid Id,
    Guid BoardId,
    Guid PersonAId,
    Guid PersonBId,
    RelationType Type,
    FuzzyDateResponse? StartDate,
    FuzzyDateResponse? EndDate,
    string? EndReason,
    string? Notes,
    DateTime CreatedAt);
