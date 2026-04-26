using ErrorOr;
using FamilyTreeApiV2.Features.Boards;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Features.Persons;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Import;

public class ImportHandler(
    ITobitAuthService tobitAuthService,
    ITobitApiService tobitApiService,
    IBoardsRepository boardsRepository,
    IMembersRepository membersRepository,
    IPersonsRepository personsRepository,
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

        using var connection = connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        var board = await boardsRepository.CreateBoardAsync("Importierter Stammbaum", connection, transaction);
        await membersRepository.AddOwnerAsync(board.Id, userId, connection, transaction);

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

            await personsRepository.CreateAsync(
                board.Id,
                personRequest,
                birthDateResult.Value?.Id,
                deathDateResult.Value?.Id,
                connection,
                transaction);
        }

        transaction.Commit();

        return new ImportResponse(board.Id, board.Name);
    }
}
