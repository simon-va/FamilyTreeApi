using ErrorOr;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Residences;

public class ResidencesHandler(
    IResidencesRepository repository,
    IFuzzyDateRepository fuzzyDateRepository,
    IDbConnectionFactory connectionFactory,
    IMembersRepository membersRepository)
{
    public async Task<ErrorOr<List<ResidenceResponse>>> GetAllAsync(Guid boardId, Guid userId)
    {
        var role = await membersRepository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return ResidencesErrors.BoardNotFound;

        var residences = await repository.GetAllAsync(boardId);
        return residences.Select(ToResidenceResponse).ToList();
    }

    public async Task<ErrorOr<ResidenceResponse>> CreateAsync(Guid boardId, CreateResidenceRequest request, Guid userId)
    {
        var role = await membersRepository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return ResidencesErrors.BoardNotFound;
        if (role is BoardRole.Viewer)
            return ResidencesErrors.Forbidden;

        var personExists = await repository.DoesPersonBelongToBoardAsync(boardId, request.PersonId);
        if (!personExists)
            return ResidencesErrors.PersonNotFound;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var startResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.StartDate, null, connection, transaction);
        if (startResult.IsError) return startResult.FirstError;

        var endResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.EndDate, null, connection, transaction);
        if (endResult.IsError) return endResult.FirstError;

        var residence = await repository.CreateAsync(
            boardId, request, startResult.Value?.Id, endResult.Value?.Id, connection, transaction);

        transaction.Commit();

        return ToResidenceResponse(residence);
    }

    public async Task<ErrorOr<ResidenceResponse>> UpdateAsync(
        Guid boardId, Guid residenceId, UpdateResidenceRequest request, Guid userId)
    {
        var role = await membersRepository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return ResidencesErrors.BoardNotFound;
        if (role is BoardRole.Viewer)
            return ResidencesErrors.Forbidden;

        var existing = await repository.GetByIdAsync(boardId, residenceId);
        if (existing is null)
            return ResidencesErrors.ResidenceNotFound;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var startResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.StartDate, existing.StartDateId, connection, transaction);
        if (startResult.IsError) return startResult.FirstError;

        var endResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.EndDate, existing.EndDateId, connection, transaction);
        if (endResult.IsError) return endResult.FirstError;

        var residence = await repository.UpdateAsync(
            boardId, residenceId, request, startResult.Value?.Id, endResult.Value?.Id, connection, transaction);
        if (residence is null)
            return ResidencesErrors.ResidenceNotFound;

        transaction.Commit();

        return ToResidenceResponse(residence);
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(Guid boardId, Guid residenceId, Guid userId)
    {
        var role = await membersRepository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return ResidencesErrors.BoardNotFound;
        if (role is BoardRole.Viewer)
            return ResidencesErrors.Forbidden;

        var deleted = await repository.DeleteAsync(boardId, residenceId);
        if (!deleted)
            return ResidencesErrors.ResidenceNotFound;

        return Result.Deleted;
    }

    private static ResidenceResponse ToResidenceResponse(Residence residence) =>
        new(residence.Id, residence.BoardId, residence.PersonId, residence.City, residence.Country,
            residence.Street, residence.Notes, residence.StartDateId, residence.EndDateId, residence.CreatedAt);
}
