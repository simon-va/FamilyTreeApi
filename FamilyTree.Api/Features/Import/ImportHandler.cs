using ErrorOr;
using FamilyTreeApiV2.Features.Boards;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Persons;
using FamilyTreeApiV2.Features.Relations;
using FamilyTreeApiV2.Features.Residences;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Import;

public class ImportHandler(
    ITobitAuthService tobitAuthService,
    ITobitApiService tobitApiService,
    IBoardsRepository boardsRepository,
    IMembersRepository membersRepository,
    IPersonsRepository personsRepository,
    IRelationsRepository relationsRepository,
    IResidencesRepository residencesRepository,
    IFuzzyDateRepository fuzzyDateRepository,
    IDbConnectionFactory connectionFactory)
{
    public async Task<ErrorOr<ImportResponse>> ImportAsync(ImportRequest request, Guid userId)
    {
        var token = await tobitAuthService.GetTokenAsync(request.Username, request.Password);
        if (token is null)
            return ImportErrors.AuthenticationFailed;

        var v1Persons = await tobitApiService.GetPersonsAsync(token);
        if (v1Persons is null)
            return ImportErrors.OldApiFailed;

        var v1Relations = await tobitApiService.GetRelationsAsync(token);
        if (v1Relations is null)
            return ImportErrors.OldApiFailed;

        var v1Residences = (await tobitApiService.GetResidencesAsync(token))?.ToList();
        if (v1Residences is null)
            return ImportErrors.OldApiFailed;

        using var connection = connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        var board = await boardsRepository.CreateBoardAsync("Importierter Stammbaum", connection, transaction);
        await membersRepository.AddOwnerAsync(board.Id, userId, connection, transaction);

        var personIdMap = new Dictionary<string, Guid>();
        foreach (var v1Person in v1Persons)
        {
            var personRequest = ImportMapper.ToCreatePersonRequest(v1Person);

            var birthDateResult = await FuzzyDateUpsertHelper.UpsertAsync(
                fuzzyDateRepository, personRequest.BirthDate, null, connection, transaction);
            if (birthDateResult.IsError)
                return birthDateResult.Errors;

            var deathDateResult = await FuzzyDateUpsertHelper.UpsertAsync(
                fuzzyDateRepository, personRequest.DeathDate, null, connection, transaction);
            if (deathDateResult.IsError)
                return deathDateResult.Errors;

            var person = await personsRepository.CreateAsync(
                board.Id,
                personRequest,
                birthDateResult.Value?.Id,
                deathDateResult.Value?.Id,
                connection,
                transaction);

            personIdMap[v1Person.Id] = person.Id;
        }

        foreach (var v1Relation in v1Relations)
        {
            if (!personIdMap.TryGetValue(v1Relation.PersonAId, out var personAId) ||
                !personIdMap.TryGetValue(v1Relation.PersonBId, out var personBId))
                continue;

            var relationRequest = ImportMapper.ToCreateRelationRequest(v1Relation, personAId, personBId);

            var startDateResult = await FuzzyDateUpsertHelper.UpsertAsync(
                fuzzyDateRepository, relationRequest.StartDate, null, connection, transaction);
            if (startDateResult.IsError)
                return startDateResult.Errors;

            var endDateResult = await FuzzyDateUpsertHelper.UpsertAsync(
                fuzzyDateRepository, relationRequest.EndDate, null, connection, transaction);
            if (endDateResult.IsError)
                return endDateResult.Errors;

            await relationsRepository.CreateAsync(
                board.Id,
                relationRequest,
                startDateResult.Value?.Id,
                endDateResult.Value?.Id,
                connection,
                transaction);
        }

        var residenceIdMap = new Dictionary<string, Guid>();
        foreach (var v1Residence in v1Residences)
        {
            if (!personIdMap.TryGetValue(v1Residence.PersonId, out var personId))
                continue;

            var residenceRequest = ImportMapper.ToCreateResidenceRequest(v1Residence, personId);

            var startDateResult = await FuzzyDateUpsertHelper.UpsertAsync(
                fuzzyDateRepository, residenceRequest.StartDate, null, connection, transaction);
            if (startDateResult.IsError)
                return startDateResult.Errors;

            var endDateResult = await FuzzyDateUpsertHelper.UpsertAsync(
                fuzzyDateRepository, residenceRequest.EndDate, null, connection, transaction);
            if (endDateResult.IsError)
                return endDateResult.Errors;

            var residence = await residencesRepository.CreateAsync(
                board.Id,
                residenceRequest,
                startDateResult.Value?.Id,
                endDateResult.Value?.Id,
                connection,
                transaction);

            residenceIdMap[v1Residence.Id] = residence.Id;
        }

        foreach (var v1Residence in v1Residences)
        {
            if (v1Residence.MovedToResidenceId is null)
                continue;

            if (!residenceIdMap.TryGetValue(v1Residence.Id, out var residenceId) ||
                !residenceIdMap.TryGetValue(v1Residence.MovedToResidenceId, out var movedToResidenceId))
                continue;

            await residencesRepository.SetMovedToResidenceIdAsync(
                board.Id, residenceId, movedToResidenceId, connection, transaction);
        }

        transaction.Commit();

        return new ImportResponse(board.Id, board.Name);
    }
}
