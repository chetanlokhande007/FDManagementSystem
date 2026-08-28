using FinTrustFDManager.Model.Entities.MasterData;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IBenchmarkRateHistoryService
    {
        Task<IEnumerable<BenchmarkRateHistory>> GetAllAsync();

        Task<IEnumerable<BenchmarkRateHistory>> GetByBenchmarkIdAsync(int benchmarkId);

        Task<BenchmarkRateHistory?> GetByIdAsync(long id);

        Task<BenchmarkRateHistory> CreateAsync(BenchmarkRateHistory model);

        Task<BenchmarkRateHistory?> UpdateAsync(long id, BenchmarkRateHistory model);

        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Gets the benchmark rate effective for a specific date.
        /// Falls back to Benchmark.CurrentRate if no history entry exists.
        /// </summary>
        Task<decimal> GetEffectiveRateAsync(int benchmarkId, DateTime asOfDate);
    }
}
