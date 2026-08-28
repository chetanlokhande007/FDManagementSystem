using FinTrustFDManager.Model.Entities.MasterData;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IBenchmarkRateHistoryRepository
    {
        Task<IEnumerable<BenchmarkRateHistory>> GetAllAsync();

        Task<IEnumerable<BenchmarkRateHistory>> GetByBenchmarkIdAsync(int benchmarkId);

        Task<BenchmarkRateHistory?> GetByBenchmarkIdAndDateAsync(int benchmarkId, DateTime asOfDate);

        Task<BenchmarkRateHistory> AddAsync(BenchmarkRateHistory model);

        Task<BenchmarkRateHistory?> UpdateAsync(BenchmarkRateHistory model);

        Task<bool> DeleteAsync(long id);
    }
}
