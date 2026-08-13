using FinTrustFDManager.Model.Entities.Investment;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IFDIdentificationService
    {
        Task<IEnumerable<FDIdentification>> GetAllAsync();

        Task<FDIdentification?> GetByIdAsync(long id);

        Task<FDIdentification> CreateAsync(FDIdentification model);

        Task<FDIdentification?> UpdateAsync(long id, FDIdentification model);

        Task<bool> DeleteAsync(long id);
    }
}