using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Residences;

public record UpdateResidenceRequest(
    string? City,
    string? Country,
    string? Street,
    string? Notes,
    FuzzyDateRequest? StartDate,
    FuzzyDateRequest? EndDate);

public record CreateResidenceRequest(
    Guid PersonId,
    string? City,
    string? Country,
    string? Street,
    string? Notes,
    FuzzyDateRequest? StartDate,
    FuzzyDateRequest? EndDate);

public record Residence(
    Guid Id,
    Guid BoardId,
    Guid PersonId,
    string? City,
    string? Country,
    string? Street,
    string? Notes,
    Guid? StartDateId,
    Guid? EndDateId,
    DateTime CreatedAt);

public record ResidenceResponse(
    Guid Id,
    Guid BoardId,
    Guid PersonId,
    string? City,
    string? Country,
    string? Street,
    string? Notes,
    Guid? StartDateId,
    Guid? EndDateId,
    DateTime CreatedAt);
