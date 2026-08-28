using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.MasterData;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Services
{
    public class BenchmarkRateHistoryService : IBenchmarkRateHistoryService
    {
        private readonly IBenchmarkRateHistoryRepository _repository;
        private readonly IBenchmarkRepository _benchmarkRepository;
        private readonly ILogger<BenchmarkRateHistoryService> _logger;

        public BenchmarkRateHistoryService(
            IBenchmarkRateHistoryRepository repository,
            IBenchmarkRepository benchmarkRepository,
            ILogger<BenchmarkRateHistoryService> logger)
        {
            _repository = repository;
            _benchmarkRepository = benchmarkRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<BenchmarkRateHistory>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IEnumerable<BenchmarkRateHistory>> GetByBenchmarkIdAsync(int benchmarkId)
        {
            return await _repository.GetByBenchmarkIdAsync(benchmarkId);
        }

        public async Task<BenchmarkRateHistory?> GetByIdAsync(long id)
        {
            var all = await _repository.GetAllAsync();
            foreach (var item in all)
            {
                if (item.BenchmarkRateHistoryId == id)
                    return item;
            }
            return null;
        }

        public async Task<BenchmarkRateHistory> CreateAsync(BenchmarkRateHistory model)
        {
            if (model.BenchmarkId <= 0)
                throw new InvalidOperationException("Benchmark ID is required.");

            if (model.EffectiveFrom == default)
                throw new InvalidOperationException("Effective From date is required.");

            // Validate no overlapping periods for the same benchmark
            var existing = await _repository.GetByBenchmarkIdAsync(model.BenchmarkId);
            foreach (var entry in existing)
            {
                if (model.EffectiveTo == null || entry.EffectiveFrom < model.EffectiveTo)
                {
                    if (entry.EffectiveTo == null || model.EffectiveFrom < entry.EffectiveTo)
                    {
                        // Check for actual overlap (not just touching boundaries)
                        bool overlaps = model.EffectiveFrom < (entry.EffectiveTo ?? DateTime.MaxValue)
                            && (model.EffectiveTo ?? DateTime.MaxValue) > entry.EffectiveFrom;
                        if (overlaps)
                        {
                            throw new InvalidOperationException(
                                $"Rate period overlaps with existing entry effective from {entry.EffectiveFrom:dd-MMM-yyyy}.");
                        }
                    }
                }
            }

            model.CreatedDate = DateTime.UtcNow;
            return await _repository.AddAsync(model);
        }

        public async Task<BenchmarkRateHistory?> UpdateAsync(long id, BenchmarkRateHistory model)
        {
            model.BenchmarkRateHistoryId = id;
            return await _repository.UpdateAsync(model);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<decimal> GetEffectiveRateAsync(int benchmarkId, DateTime asOfDate)
        {
            // First try to find a historical rate for this date
            var historyEntry = await _repository.GetByBenchmarkIdAndDateAsync(benchmarkId, asOfDate);
            if (historyEntry != null)
            {
                return historyEntry.Rate;
            }

            // Fall back to the benchmark's current rate
            var benchmark = await _benchmarkRepository.GetByIdAsync(benchmarkId);
            return benchmark?.CurrentRate ?? 0m;
        }
    }
}
