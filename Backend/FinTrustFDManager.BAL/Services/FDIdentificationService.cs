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

        public FDIdentificationService(
            IFDIdentificationRepository repository,
            IFDInterestRepository interestRepository,
            IFDCashFlowRepository cashFlowRepository)
        {
            _repository = repository;
            _interestRepository = interestRepository;
            _cashFlowRepository = cashFlowRepository;
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

            return await _repository.UpdateAsync(model);
        }

        // ==============================
        // DELETE FD
        // ==============================

        public async Task<bool> DeleteAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }

        // ==============================
        // FD LANDING PAGE
        // ==============================

        public async Task<IEnumerable<FDLandingDto>>
            GetLandingDataAsync()
        {
            var fdList = await _repository.GetAllAsync();

            var result = new List<FDLandingDto>();

            foreach (var fd in fdList)
            {
                var interest =
                    await _interestRepository.GetByFdIdAsync(fd.FdId);

                var cashFlows =
                    await _cashFlowRepository.GetByFdIdAsync(fd.FdId);

                var data = new FDLandingDto
                {
                    // FD Identification
                    FdId = fd.FdId,
                    FdReferenceNo = fd.FdReferenceNo,
                    EntityId = fd.EntityId,
                    CounterpartyId = fd.CounterpartyId,
                    CurrencyCode = fd.CurrencyCode,
                    PrincipalAmount = fd.PrincipalAmount,
                    StartDate = fd.StartDate,
                    EndDate = fd.EndDate,
                    SettlementDate = fd.SettlementDate,
                    Status = fd.Status,

                    // Interest
                    InterestRate = interest?.InterestRate ?? 0,
                    InterestRateType =
                        interest?.InterestRateType ?? string.Empty,

                    InterestFrequency =
                        interest?.InterestFrequency ?? string.Empty,

                    CompoundingFrequency =
                        interest?.CompoundingFrequency ?? string.Empty,

                    CalculationBasis =
                        interest?.CalculationBasis ?? string.Empty,

                    // Cash Flow
                    TotalPrincipal =
                        cashFlows?.Sum(x => x.PrincipalAmount) ?? 0,

                    TotalGrossInterest =
                        cashFlows?.Sum(x => x.GrossInterest) ?? 0,

                    TotalTds =
                        cashFlows?.Sum(x => x.TdsAmount) ?? 0,

                    TotalNetInterest =
                        cashFlows?.Sum(x => x.NetInterest) ?? 0,

                    TotalAmount =
                        cashFlows?.Sum(x => x.TotalAmount) ?? 0
                };

                result.Add(data);
            }

            return result;
        }
    }
}