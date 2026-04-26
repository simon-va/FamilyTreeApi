using System.Data;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Relations;

public interface IRelationsRepository
{
    Task<IEnumerable<(Relation Relation, FuzzyDate? StartDate, FuzzyDate? EndDate)>> GetAllAsync(Guid boardId);

    Task<bool> DoPersonsBelongToBoardAsync(Guid boardId, Guid personAId, Guid personBId);

    Task<Relation?> GetByIdAsync(Guid boardId, Guid relationId);

    Task<Relation> CreateAsync(
        Guid boardId,
        CreateRelationRequest request,
        Guid? startDateId,
        Guid? endDateId,
        IDbConnection connection,
        IDbTransaction transaction);

    Task<Relation?> UpdateAsync(
        Guid boardId,
        Guid relationId,
        UpdateRelationRequest request,
        Guid? startDateId,
        Guid? endDateId,
        IDbConnection connection,
        IDbTransaction transaction);

    Task<bool> DeleteAsync(Guid boardId, Guid relationId);
}
