using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.MasterData;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Services
{
    public class BenchmarkService : IBenchmarkService
    {
        private readonly IBenchmarkRepository _repository;
        private readonly ILogger<BenchmarkService> _logger;

        public BenchmarkService(IBenchmarkRepository repository, ILogger<BenchmarkService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<Benchmark>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Benchmark?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Benchmark> CreateAsync(Benchmark model)
        {
            if (string.IsNullOrWhiteSpace(model.BenchmarkName))
                throw new InvalidOperationException("Benchmark Name is required.");

            model.CreatedDate = DateTime.UtcNow;
            return await _repository.AddAsync(model);
        }

        public async Task<Benchmark?> UpdateAsync(int id, Benchmark model)
        {
            if (string.IsNullOrWhiteSpace(model.BenchmarkName))
                throw new InvalidOperationException("Benchmark Name is required.");

            model.BenchmarkId = id;
            return await _repository.UpdateAsync(model);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
