using FinTrustFDManager.BAL.DTOs;
using FinTrustFDManager.Model.Entities.Investment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IFDInterestService
    {
        Task<IEnumerable<FDInterest>> GetAllAsync();

        Task<FDInterest?> GetByIdAsync(long id);

        Task<FDInterest?> GetByFdIdAsync(long fdId);

        Task<FDInterest> CreateAsync(FDInterest model);

        Task<FDInterest?> UpdateAsync(long id, FDInterest model);

        Task<bool> DeleteAsync(long id);

        Task<bool> RegenerateCashFlowsAsync(long fdId);

        /// <summary>
        /// P0-2 (BUG-002): authoritative cash-flow summary for an FD.
        /// Single source of truth for the Cash Flow tab metadata
        /// (rate, compounding, basis, tenor, reference) and schedule.
        /// Throws KeyNotFoundException when the FD does not exist.
        /// </summary>
        Task<FDCashFlowSummaryDto> GetSummaryAsync(long fdId);
    }
}
