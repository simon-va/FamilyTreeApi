using ErrorOr;
using FamilyTreeApiV2.Features.Boards;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Persons;
using FamilyTreeApiV2.Features.Relations;
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

        transaction.Commit();

        return new ImportResponse(board.Id, board.Name);
    }
}
