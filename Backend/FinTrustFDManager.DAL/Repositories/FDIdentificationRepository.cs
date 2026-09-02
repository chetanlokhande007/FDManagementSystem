using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
                .Include(x => x.Entity)
                .Include(x => x.CounterParty)
                .Include(x => x.CurrencyNavigation)

                .ToListAsync();
        }

        public async Task<FDIdentification?> GetByIdAsync(long id)
        {
            return await _context.FDIdentifications
                .AsNoTracking()
                .Include(x => x.Entity)
                .Include(x => x.CounterParty)
                .Include(x => x.CurrencyNavigation)

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

        public async Task<string> GetNextFdReferenceNoAsync()
        {
            // Use PostgreSQL atomic sequence to generate unique FD reference numbers.
            // This avoids the race condition of GetLastAsync() + increment.
            var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT nextval('fd_reference_seq')";
                var result = await cmd.ExecuteScalarAsync();
                long seqValue = Convert.ToInt64(result);
                return $"FD-{seqValue:D4}";
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        public async Task AddApprovalHistoryAsync(FDApprovalHistory history)
        {
            _context.FDApprovalHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<FDApprovalHistory>> GetApprovalHistoryAsync(long fdId)
        {
            return await _context.FDApprovalHistories
                .AsNoTracking()
                .Where(x => x.FdId == fdId)
                .OrderBy(x => x.ActionDate)
                .ToListAsync();
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
            existing.CurrencyId = model.CurrencyId;
            existing.PrincipalAmount = model.PrincipalAmount;
            existing.StartDate = model.StartDate;
            existing.EndDate = model.EndDate;
            existing.SettlementDate = model.SettlementDate;

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
                    fd.CurrencyId,
                    CurrencyCode = _context.Currencies.Where(c => c.CurrencyId == fd.CurrencyId).Select(c => c.CurrencyCode).FirstOrDefault() ?? string.Empty,
                    fd.PrincipalAmount,
                    fd.StartDate,
                    fd.EndDate,
                    fd.SettlementDate,
                    fd.Status,
                    InterestRate = i != null ? i.InterestRate : 0m,
                    InterestRateType = i != null ? i.InterestRateType : string.Empty,
                    InterestFrequency = i != null ? _context.InterestFrequencies.Where(f => f.Id == i.InterestFrequencyId).Select(f => f.FrequencyName).FirstOrDefault() ?? string.Empty : string.Empty,
                    IsCompounding = i != null && i.IsCompounding,
                    CompoundingFrequency = i != null && i.IsCompounding && i.CompoundingFrequencyId.HasValue
                        ? (_context.InterestFrequencies.Where(f => f.Id == i.CompoundingFrequencyId.Value).Select(f => f.FrequencyName).FirstOrDefault() ?? "Not Applicable")
                        : "Not Applicable",
                    CalculationBasis = i != null ? _context.DayCountConventions.Where(d => d.Id == i.DayCountConventionId).Select(d => d.ConventionName).FirstOrDefault() ?? string.Empty : string.Empty
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
                // If no cash flows exist for this FD (e.g. interest not yet configured),
                // grossInterest should be 0 — not negative.
                cashFlowAggregates.TryGetValue(fd.FdId, out var totalInflows);
                var grossInterest = totalInflows > 0
                    ? totalInflows - fd.PrincipalAmount
                    : 0m;
                return new FDLandingDto
                {
                    FdId = fd.FdId,
                    FdReferenceNo = fd.FdReferenceNo,
                    EntityId = fd.EntityId,
                    EntityName = fd.EntityName,
                    CounterpartyId = fd.CounterpartyId,
                    CounterPartyName = fd.CounterPartyName,
                    CurrencyCode = fd.CurrencyCode,
                    CurrencyId = fd.CurrencyId,
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

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.FDIdentifications
                .AsNoTracking()
                .CountAsync(fd => fd.Status == FDStatus.PendingFdAdmin || fd.Status == FDStatus.PendingCa);
        }

        public async Task<IEnumerable<FDLandingDto>> GetPendingApprovalsAsync()
        {
            var pendingFDs = await (
                from fd in _context.FDIdentifications.AsNoTracking()
                    .Where(fd => fd.Status == FDStatus.PendingFdAdmin || fd.Status == FDStatus.PendingCa)
                join ent in _context.Entities on fd.EntityId equals ent.EntityId into entGroup
                from e in entGroup.DefaultIfEmpty()
                join cp in _context.CounterParties on fd.CounterpartyId equals cp.CounterPartyId into cpGroup
                from c in cpGroup.DefaultIfEmpty()
                join cur in _context.Currencies on fd.CurrencyId equals cur.CurrencyId into curGroup
                from cr in curGroup.DefaultIfEmpty()
                join intEntry in _context.FDInterests
                    on fd.FdId equals intEntry.FdId into interestGroup
                from i in interestGroup.DefaultIfEmpty()
                join user in _context.Users
                    on fd.CreatedBy equals user.Id into userGroup
                from u in userGroup.DefaultIfEmpty()
                orderby fd.CreatedDate descending
                select new FDLandingDto
                {
                    FdId = fd.FdId,
                    FdReferenceNo = fd.FdReferenceNo,
                    EntityId = fd.EntityId,
                    EntityName = e != null ? e.EntityName : string.Empty,
                    CounterpartyId = fd.CounterpartyId,
                    CounterPartyName = c != null ? c.CounterPartyName : string.Empty,
                    CurrencyId = fd.CurrencyId,
                    CurrencyCode = cr != null ? cr.CurrencyCode : string.Empty,
                    PrincipalAmount = fd.PrincipalAmount,
                    StartDate = fd.StartDate,
                    EndDate = fd.EndDate,
                    SettlementDate = fd.SettlementDate,
                    Status = fd.Status,
                    InterestRate = i != null ? i.InterestRate : 0m,
                    InterestRateType = i != null ? i.InterestRateType : string.Empty,
                    InterestFrequency = i != null ? _context.InterestFrequencies.Where(f => f.Id == i.InterestFrequencyId).Select(f => f.FrequencyName).FirstOrDefault() ?? string.Empty : string.Empty,
                    CompoundingFrequency = i != null && i.IsCompounding && i.CompoundingFrequencyId.HasValue
                        ? (_context.InterestFrequencies.Where(f => f.Id == i.CompoundingFrequencyId.Value).Select(f => f.FrequencyName).FirstOrDefault() ?? "Not Applicable")
                        : "Not Applicable",
                    CalculationBasis = i != null ? _context.DayCountConventions.Where(d => d.Id == i.DayCountConventionId).Select(d => d.ConventionName).FirstOrDefault() ?? string.Empty : string.Empty,
                    TotalPrincipal = fd.PrincipalAmount,
                    TotalGrossInterest = 0,
                    TotalTds = 0,
                    TotalNetInterest = 0,
                    TotalAmount = fd.PrincipalAmount
                }
            ).ToListAsync();

            return pendingFDs;
        }

        public async Task<int> GetCriticalPendingCountAsync(decimal criticalThreshold)
        {
            return await _context.FDIdentifications
                .AsNoTracking()
                .Where(fd => (fd.Status == FDStatus.PendingFdAdmin || fd.Status == FDStatus.PendingCa) && fd.PrincipalAmount >= criticalThreshold)
                .CountAsync();
        }

        public async Task<int> GetApprovedTodayCountAsync(long approverUserId)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            return await _context.FDApprovalHistories
                .AsNoTracking()
                .Where(h => h.ActionBy == approverUserId
                    && h.Action == "APPROVE"
                    && h.ActionDate >= today
                    && h.ActionDate < tomorrow)
                .CountAsync();
        }

        public async Task<Dictionary<string, int>> GetStatusCountsAsync()
        {
            return await _context.FDIdentifications
                .AsNoTracking()
                .GroupBy(fd => fd.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        public async Task<int> GetRejectedTodayCountAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            return await _context.FDApprovalHistories
                .AsNoTracking()
                .Where(h => h.Action == "REJECT"
                    && h.ActionDate >= today
                    && h.ActionDate < tomorrow)
                .CountAsync();
        }

        public async Task<IEnumerable<FDLandingDto>> GetAdminApprovalListAsync(string? statusFilter)
        {
            var query = from fd in _context.FDIdentifications.AsNoTracking()
                join ent in _context.Entities on fd.EntityId equals ent.EntityId into entGroup
                from e in entGroup.DefaultIfEmpty()
                join cp in _context.CounterParties on fd.CounterpartyId equals cp.CounterPartyId into cpGroup
                from c in cpGroup.DefaultIfEmpty()
                join cur in _context.Currencies on fd.CurrencyId equals cur.CurrencyId into curGroup
                from cr in curGroup.DefaultIfEmpty()
                join intEntry in _context.FDInterests
                    on fd.FdId equals intEntry.FdId into interestGroup
                from i in interestGroup.DefaultIfEmpty()
                select new FDLandingDto
                {
                    FdId = fd.FdId,
                    FdReferenceNo = fd.FdReferenceNo,
                    EntityId = fd.EntityId,
                    EntityName = e != null ? e.EntityName : string.Empty,
                    CounterpartyId = fd.CounterpartyId,
                    CounterPartyName = c != null ? c.CounterPartyName : string.Empty,
                    CurrencyId = fd.CurrencyId,
                    CurrencyCode = cr != null ? cr.CurrencyCode : string.Empty,
                    PrincipalAmount = fd.PrincipalAmount,
                    StartDate = fd.StartDate,
                    EndDate = fd.EndDate,
                    SettlementDate = fd.SettlementDate,
                    Status = fd.Status,
                    InterestRate = i != null ? i.InterestRate : 0m,
                    InterestRateType = i != null ? i.InterestRateType : string.Empty,
                    InterestFrequency = i != null ? _context.InterestFrequencies.Where(f => f.Id == i.InterestFrequencyId).Select(f => f.FrequencyName).FirstOrDefault() ?? string.Empty : string.Empty,
                    CompoundingFrequency = i != null && i.IsCompounding && i.CompoundingFrequencyId.HasValue
                        ? (_context.InterestFrequencies.Where(f => f.Id == i.CompoundingFrequencyId.Value).Select(f => f.FrequencyName).FirstOrDefault() ?? "Not Applicable")
                        : "Not Applicable",
                    CalculationBasis = i != null ? _context.DayCountConventions.Where(d => d.Id == i.DayCountConventionId).Select(d => d.ConventionName).FirstOrDefault() ?? string.Empty : string.Empty,
                    TotalPrincipal = fd.PrincipalAmount,
                    TotalGrossInterest = 0,
                    TotalTds = 0,
                    TotalNetInterest = 0,
                    TotalAmount = fd.PrincipalAmount
                };

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(x => x.Status == statusFilter);
            }

            return await query.OrderByDescending(x => x.FdId).ToListAsync();
        }

        public async Task<string> GetUserNameAsync(long userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            return user?.FullName ?? $"User #{userId}";
        }
    }
}