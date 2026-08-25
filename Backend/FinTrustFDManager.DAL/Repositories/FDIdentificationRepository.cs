using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinTrustFDManager.DAL.Repositories
{
    public class FDIdentificationRepository : IFDIdentificationRepository
    {
        private readonly ApplicationDbContext _context;

        public FDIdentificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FDIdentification>> GetAllAsync()
        {
            return await _context.FDIdentifications
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<FDIdentification?> GetByIdAsync(long id)
        {
            return await _context.FDIdentifications
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FdId == id);
        }

        public async Task<FDIdentification> AddAsync(FDIdentification model)
        {
            _context.FDIdentifications.Add(model);

            await _context.SaveChangesAsync();

            return model;
        }
        public async Task<FDIdentification?> GetLastAsync()
        {
            return await _context.FDIdentifications
                .AsNoTracking()
                .OrderByDescending(x => x.FdId)
                .FirstOrDefaultAsync();
        }
        public async Task<FDIdentification?> UpdateAsync(
            FDIdentification model)
        {
            var existing = await _context.FDIdentifications
                .FirstOrDefaultAsync(x => x.FdId == model.FdId);

            if (existing == null)
                return null;

            // DO NOT update FdReferenceNo since it's auto-generated and immutable
            existing.EntityId = model.EntityId;
            existing.CounterpartyId = model.CounterpartyId;
            existing.CurrencyCode = model.CurrencyCode;
            existing.PrincipalAmount = model.PrincipalAmount;
            existing.StartDate = model.StartDate;
            existing.EndDate = model.EndDate;
            existing.SettlementDate = model.SettlementDate;
            existing.BankAccountId = model.BankAccountId;
            existing.Status = model.Status;
            existing.Remarks = model.Remarks;
            existing.ModifiedBy = model.ModifiedBy;
            existing.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _context.FDIdentifications
                .FirstOrDefaultAsync(x => x.FdId == id);

            if (existing == null)
                return false;

            _context.FDIdentifications.Remove(existing);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangeStatusAsync(long id, string status)
        {
            var existing = await _context.FDIdentifications
                .FirstOrDefaultAsync(x => x.FdId == id);

            if (existing == null)
                return false;

            existing.Status = status;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<FDLandingDto>> GetLandingDataAsync()
        {
            // ── Query 1: FD + Interest + Entity + Counterparty (LEFT JOINs, 1 SQL) ──
            var fdWithInterest = await (
                from fd in _context.FDIdentifications.AsNoTracking()
                join ent in _context.Entities on fd.EntityId equals ent.EntityId into entGroup
                from e in entGroup.DefaultIfEmpty()
                join cp in _context.CounterParties on fd.CounterpartyId equals cp.CounterPartyId into cpGroup
                from c in cpGroup.DefaultIfEmpty()
                join intEntry in _context.FDInterests
                    on fd.FdId equals intEntry.FdId
                    into interestGroup
                from i in interestGroup.DefaultIfEmpty()
                select new
                {
                    fd.FdId,
                    fd.FdReferenceNo,
                    fd.EntityId,
                    EntityName = e != null ? e.EntityName : string.Empty,
                    fd.CounterpartyId,
                    CounterPartyName = c != null ? c.CounterPartyName : string.Empty,
                    fd.CurrencyCode,
                    fd.PrincipalAmount,
                    fd.StartDate,
                    fd.EndDate,
                    fd.SettlementDate,
                    fd.Status,
                    InterestRate = i != null ? i.InterestRate : 0m,
                    InterestRateType = i != null ? i.InterestRateType : string.Empty,
                    InterestFrequency = i != null ? i.InterestFrequency : string.Empty,
                    IsCompounding = i != null && i.IsCompounding,
                    CompoundingFrequency = i != null && i.IsCompounding
                        ? (i.CompoundingFrequency ?? "Not Applicable")
                        : "Not Applicable",
                    CalculationBasis = i != null ? i.CalculationBasis : string.Empty
                }
            ).ToListAsync();

            // ── Query 2: Cash flow aggregates (GROUP BY, 1 SQL) ──
            var cashFlowAggregates = await _context.FDCashFlows
                .AsNoTracking()
                .Where(cf => cf.Direction == "INFLOW")
                .GroupBy(cf => cf.FdId)
                .Select(g => new
                {
                    FdId = g.Key,
                    TotalInflows = g.Sum(cf => cf.CashFlowAmount)
                })
                .ToDictionaryAsync(x => x.FdId, x => x.TotalInflows);

            // ── Assemble DTOs in memory ──
            return fdWithInterest.Select(fd =>
            {
                cashFlowAggregates.TryGetValue(fd.FdId, out var totalInflows);
                var grossInterest = totalInflows - fd.PrincipalAmount;
                return new FDLandingDto
                {
                    FdId = fd.FdId,
                    FdReferenceNo = fd.FdReferenceNo,
                    EntityId = fd.EntityId,
                    EntityName = fd.EntityName,
                    CounterpartyId = fd.CounterpartyId,
                    CounterPartyName = fd.CounterPartyName,
                    CurrencyCode = fd.CurrencyCode,
                    PrincipalAmount = fd.PrincipalAmount,
                    StartDate = fd.StartDate,
                    EndDate = fd.EndDate,
                    SettlementDate = fd.SettlementDate,
                    Status = fd.Status,
                    InterestRate = fd.InterestRate,
                    InterestRateType = fd.InterestRateType,
                    InterestFrequency = fd.InterestFrequency,
                    CompoundingFrequency = fd.CompoundingFrequency,
                    CalculationBasis = fd.CalculationBasis,
                    TotalPrincipal = fd.PrincipalAmount,
                    TotalGrossInterest = grossInterest,
                    TotalTds = 0,
                    TotalNetInterest = grossInterest,
                    TotalAmount = fd.PrincipalAmount + grossInterest
                };
            }).ToList();
        }
    }
}