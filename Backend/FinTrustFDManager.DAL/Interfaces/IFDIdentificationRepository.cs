using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.Investment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IFDIdentificationRepository
    {
        Task<IEnumerable<FDIdentification>> GetAllAsync();

        Task<FDIdentification?> GetByIdAsync(long id);

        Task<FDIdentification> AddAsync(FDIdentification model);

        Task<FDIdentification?> UpdateAsync(FDIdentification model);
        Task<FDIdentification?> GetLastAsync();
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Returns FD landing data in a single optimized query
        /// (joins FDIdentification + FDInterest + aggregated FDCashFlow).
        /// Replaces the N+1 query pattern.
        /// </summary>
        Task<IEnumerable<FDLandingDto>> GetLandingDataAsync();

        /// <summary>
        /// Gets the next FD reference number atomically using a PostgreSQL sequence.
        /// Returns the formatted reference (e.g. FD-0001).
        /// </summary>
        Task<string> GetNextFdReferenceNoAsync();

        /// <summary>
        /// Adds an approval history record.
        /// </summary>
        Task AddApprovalHistoryAsync(FDApprovalHistory history);

        /// <summary>
        /// Gets approval history for an FD.
        /// </summary>
        Task<IEnumerable<FDApprovalHistory>> GetApprovalHistoryAsync(long fdId);
    }
}
