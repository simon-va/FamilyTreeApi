using ErrorOr;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Relations;

public class RelationsHandler(
    IRelationsRepository repository,
    IFuzzyDateRepository fuzzyDateRepository,
    IDbConnectionFactory connectionFactory,
    IMembersRepository membersRepository)
{
    public async Task<ErrorOr<List<RelationResponse>>> GetAllAsync(Guid boardId, Guid userId)
    {
        var role = await membersRepository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return RelationsErrors.BoardNotFound;

        var relations = await repository.GetAllAsync(boardId);
        return relations.Select(r => ToRelationResponse(r.Relation, r.StartDate, r.EndDate)).ToList();
    }

    public async Task<ErrorOr<RelationResponse>> CreateAsync(
        Guid boardId, CreateRelationRequest request, Guid userId)
    {
        var role = await membersRepository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return RelationsErrors.BoardNotFound;
        if (role is BoardRole.Viewer)
            return RelationsErrors.Forbidden;

        var personsExist = await repository.DoPersonsBelongToBoardAsync(boardId, request.PersonAId, request.PersonBId);
        if (!personsExist)
            return RelationsErrors.PersonNotFound;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var startResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.StartDate, null, connection, transaction);
        if (startResult.IsError) return startResult.FirstError;

        var endResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.EndDate, null, connection, transaction);
        if (endResult.IsError) return endResult.FirstError;

        var relation = await repository.CreateAsync(
            boardId, request, startResult.Value?.Id, endResult.Value?.Id, connection, transaction);

        transaction.Commit();

        return ToRelationResponse(relation, startResult.Value, endResult.Value);
    }

    public async Task<ErrorOr<RelationResponse>> UpdateAsync(
        Guid boardId, Guid relationId, UpdateRelationRequest request, Guid userId)
    {
        var role = await membersRepository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return RelationsErrors.BoardNotFound;
        if (role is BoardRole.Viewer)
            return RelationsErrors.Forbidden;

        var existing = await repository.GetByIdAsync(boardId, relationId);
        if (existing is null)
            return RelationsErrors.RelationNotFound;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var startResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.StartDate, existing.StartDateId, connection, transaction);
        if (startResult.IsError) return startResult.FirstError;

        var endResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.EndDate, existing.EndDateId, connection, transaction);
        if (endResult.IsError) return endResult.FirstError;

        var relation = await repository.UpdateAsync(
            boardId, relationId, request, startResult.Value?.Id, endResult.Value?.Id, connection, transaction);
        if (relation is null)
            return RelationsErrors.RelationNotFound;

        transaction.Commit();

        return ToRelationResponse(relation, startResult.Value, endResult.Value);
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(Guid boardId, Guid relationId, Guid userId)
    {
        var role = await membersRepository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return RelationsErrors.BoardNotFound;
        if (role is BoardRole.Viewer)
            return RelationsErrors.Forbidden;

        var deleted = await repository.DeleteAsync(boardId, relationId);
        if (!deleted)
            return RelationsErrors.RelationNotFound;

        return Result.Deleted;
    }

    private static RelationResponse ToRelationResponse(Relation relation, FuzzyDate? startDate, FuzzyDate? endDate) =>
        new(relation.Id, relation.BoardId, relation.PersonAId, relation.PersonBId, relation.Type,
            ToFuzzyDateResponse(startDate), ToFuzzyDateResponse(endDate),
            relation.EndReason, relation.Notes, relation.CreatedAt);

    private static FuzzyDateResponse? ToFuzzyDateResponse(FuzzyDate? fuzzyDate) =>
        fuzzyDate is null ? null
            : new(fuzzyDate.Id, fuzzyDate.Precision, fuzzyDate.Date, fuzzyDate.DatePrecision,
                  fuzzyDate.DateTo, fuzzyDate.DateToPrecision, fuzzyDate.Note);
}
