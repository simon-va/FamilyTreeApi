using System.Data;

namespace FamilyTreeApiV2.Features.Residences;

public interface IResidencesRepository
{
    Task<bool> DoesPersonBelongToBoardAsync(Guid boardId, Guid personId);
    Task<IEnumerable<Residence>> GetAllAsync(Guid boardId);
    Task<Residence?> GetByIdAsync(Guid boardId, Guid residenceId);
    Task<Residence> CreateAsync(Guid boardId, CreateResidenceRequest request, Guid? startDateId, Guid? endDateId, IDbConnection connection, IDbTransaction transaction);
    Task<Residence?> UpdateAsync(Guid boardId, Guid residenceId, UpdateResidenceRequest request, Guid? startDateId, Guid? endDateId, IDbConnection connection, IDbTransaction transaction);
    Task<bool> DeleteAsync(Guid boardId, Guid residenceId);
    Task SetMovedToResidenceIdAsync(Guid boardId, Guid residenceId, Guid movedToResidenceId, IDbConnection connection, IDbTransaction transaction);
}
