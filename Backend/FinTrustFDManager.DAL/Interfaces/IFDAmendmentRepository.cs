using FinTrustFDManager.Model.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IFDAmendmentRepository
    {
        Task<FDAmendment?> GetByIdAsync(long amendmentId);
        Task<IEnumerable<FDAmendment>> GetByFdIdAsync(long fdId);
        Task<FDAmendment?> GetPendingByFdIdAsync(long fdId);
        Task<FDAmendment> AddAsync(FDAmendment amendment);
        Task<FDAmendment?> UpdateAsync(FDAmendment amendment);
    }
}
