using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities.Investment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Services
{
    public class FDIdentificationService : IFDIdentificationService
    {
        private readonly IFDIdentificationRepository _repository;
        private readonly IFDInterestRepository _interestRepository;
        private readonly IFDCashFlowRepository _cashFlowRepository;
        private readonly IFDInterestService _interestService;

        public FDIdentificationService(
            IFDIdentificationRepository repository,
            IFDInterestRepository interestRepository,
            IFDCashFlowRepository cashFlowRepository,
            IFDInterestService interestService)
        {
            _repository = repository;
            _interestRepository = interestRepository;
            _cashFlowRepository = cashFlowRepository;
            _interestService = interestService;
        }

        // ==============================
        // GET ALL FD
        // ==============================

        public async Task<IEnumerable<FDIdentification>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        // ==============================
        // GET FD BY ID
        // ==============================

        public async Task<FDIdentification?> GetByIdAsync(long id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // ==============================
        // CREATE FD
        // ==============================

        public async Task<FDIdentification> CreateAsync(
            FDIdentification model)
        {
            var lastFd = await _repository.GetLastAsync();

            long nextNumber = 1;

            if (lastFd != null &&
                !string.IsNullOrWhiteSpace(lastFd.FdReferenceNo))
            {
                // Example: FD-0001
                string numberPart = lastFd.FdReferenceNo
                    .Replace("FD-", "");

                if (long.TryParse(
                    numberPart,
                    out long lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            model.FdReferenceNo = $"FD-{nextNumber:D4}";

            model.Status = "DRAFT";

            model.CreatedDate = DateTime.UtcNow;

            // Ensure DateTime fields are UTC for PostgreSQL timestamp with time zone
            model.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
            model.EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);
            if (model.SettlementDate.HasValue)
                model.SettlementDate = DateTime.SpecifyKind(model.SettlementDate.Value, DateTimeKind.Utc);

            return await _repository.AddAsync(model);
        }

        // ==============================
        // UPDATE FD
        // ==============================

        public async Task<FDIdentification?> UpdateAsync(
            long id,
            FDIdentification model)
        {
            model.FdId = id;

            // Ensure DateTime fields are UTC for PostgreSQL timestamp with time zone
            model.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
            model.EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);
            if (model.SettlementDate.HasValue)
                model.SettlementDate = DateTime.SpecifyKind(model.SettlementDate.Value, DateTimeKind.Utc);
            model.ModifiedDate = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(model);

            if (result != null)
            {
                await _interestService.RegenerateCashFlowsAsync(result.FdId);
            }

            return result;
        }

        // ==============================
        // DELETE FD
        // ==============================

        public async Task<bool> DeleteAsync(long id)
        {
            // Delete cash flows in bulk (cascade would handle this, but we do it explicitly
            // to ensure application-level consistency)
            var cashFlows = (await _cashFlowRepository.GetByFdIdAsync(id)).ToList();
            if (cashFlows.Count > 0)
            {
                await _cashFlowRepository.DeleteRangeAsync(cashFlows);
            }

            // Delete interest
            var interest = await _interestRepository.GetByFdIdAsync(id);
            if (interest != null)
            {
                await _interestRepository.DeleteAsync(interest.FdInterestId);
            }

            return await _repository.DeleteAsync(id);
        }

        // ==============================
        // FD LANDING PAGE
        // ==============================

        public async Task<IEnumerable<FDLandingDto>>
            GetLandingDataAsync()
        {
            // Uses a single optimized query (2 SQL statements) instead of
            // the previous N+1 pattern (1 + 2N SQL statements).
            return await _repository.GetLandingDataAsync();
        }

        public async Task<bool> ChangeStatusAsync(long id, string status)
        {
            return await _repository.ChangeStatusAsync(id, status);
        }
    }
}