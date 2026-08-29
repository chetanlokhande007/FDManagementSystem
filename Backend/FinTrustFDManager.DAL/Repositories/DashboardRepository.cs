using System;
using System.Linq;
using System.Threading.Tasks;
using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Dashboard;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public DashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            // ── Active FD count & principal ──
            var activeFDs = await _context.FDIdentifications
                .AsNoTracking()
                .Include(f => f.Entity)
                .Include(f => f.Bank)
                .Where(f => f.Status == "APPROVED" || f.Status == "DRAFT")
                .ToListAsync();

            var activeFDCount = activeFDs.Count;
            var totalPrincipal = activeFDs.Sum(f => f.PrincipalAmount);

            // ── Accrued interest: Total Inflows − Principal ──
            // This is the financially correct aggregation that avoids
            // double-counting when Interest and Compounding events share a date.
            var totalInflows = await _context.FDCashFlows
                .AsNoTracking()
                .Where(cf => cf.Direction == "INFLOW")
                .SumAsync(cf => cf.CashFlowAmount);

            var totalOutflows = await _context.FDCashFlows
                .AsNoTracking()
                .Where(cf => cf.Direction == "OUTFLOW")
                .SumAsync(cf => cf.CashFlowAmount);

            var totalAccruedInterest = totalInflows - totalOutflows;

            // ── Maturity amounts per FD ──
            // For each FD, sum all INFLOW CashFlowAmounts occurring on the
            // Maturity date. This correctly includes both the principal return
            // AND any final interest payout on the same date.
            var maturityEndDates = await _context.FDCashFlows
                .AsNoTracking()
                .Where(cf => cf.Event == "Maturity")
                .GroupBy(cf => cf.FdId)
                .Select(g => new { FdId = g.Key, MaturityEndDate = g.Max(cf => cf.EndDate) })
                .ToDictionaryAsync(x => x.FdId, x => x.MaturityEndDate);

            var maturityAmounts = new System.Collections.Generic.Dictionary<long, decimal>();
            if (maturityEndDates.Count > 0)
            {
                var fdIdsWithMaturity = maturityEndDates.Keys.ToList();
                var maturityDatePairs = maturityEndDates.ToList();

                // Batch query: get all INFLOW cash flows on each FD's maturity date
                var allMaturityCashFlows = await _context.FDCashFlows
                    .AsNoTracking()
                    .Where(cf => fdIdsWithMaturity.Contains(cf.FdId)
                                 && cf.Direction == "INFLOW")
                    .ToListAsync();

                foreach (var pair in maturityDatePairs)
                {
                    var amt = allMaturityCashFlows
                        .Where(cf => cf.FdId == pair.Key && cf.EndDate == pair.Value)
                        .Sum(cf => cf.CashFlowAmount);
                    maturityAmounts[pair.Key] = amt;
                }
            }

            // ── Upcoming maturities (next 30 days) ──
            var upcomingFDs = activeFDs
                .Where(f => f.EndDate.Date >= today && f.EndDate.Date <= today.AddDays(30))
                .OrderBy(f => f.EndDate)
                .ToList();

            var upcomingMaturities = upcomingFDs.Select(f =>
            {
                var daysUntil = (f.EndDate.Date - today).Days;
                var maturityAmt = maturityAmounts.TryGetValue(f.FdId, out var amt) ? amt : f.PrincipalAmount;
                string status;
                if (daysUntil < 0) status = "Matured";
                else if (daysUntil == 0) status = "Due Today";
                else status = $"Due in {daysUntil} days";

                return new FDUpcomingMaturityDto
                {
                    FdId = f.FdId,
                    FdReferenceNo = f.FdReferenceNo,
                    BankName = f.Bank?.BankName ?? "",
                    PrincipalAmount = f.PrincipalAmount,
                    MaturityDate = f.EndDate,
                    MaturityAmount = maturityAmt,
                    Status = status
                };
            }).ToList();

            // ── Maturing this month ──
            var maturingThisMonthFDs = activeFDs
                .Where(f => f.EndDate.Date >= monthStart && f.EndDate.Date < monthEnd)
                .ToList();

            var maturingThisMonthCount = maturingThisMonthFDs.Count;
            var maturingThisMonthValue = maturingThisMonthFDs
                .Sum(f => maturityAmounts.TryGetValue(f.FdId, out var amt) ? amt : f.PrincipalAmount);

            // ── Recently added FDs (last 5) ──
            var recentFDs = activeFDs
                .OrderByDescending(f => f.CreatedDate)
                .Take(5)
                .ToList();

            // Batch-fetch interest records for recent FDs (avoids N+1)
            var recentFdIds = recentFDs.Select(f => f.FdId).ToList();
            var recentInterests = await _context.FDInterests
                .AsNoTracking()
                .Where(i => recentFdIds.Contains(i.FdId))
                .ToDictionaryAsync(i => i.FdId, i => i);

            var recentlyAdded = recentFDs.Select(f =>
            {
                recentInterests.TryGetValue(f.FdId, out var interest);

                return new FDRecentDto
                {
                    FdId = f.FdId,
                    FdReferenceNo = f.FdReferenceNo,
                    StartDate = f.StartDate,
                    PrincipalAmount = f.PrincipalAmount,
                    InterestRate = interest?.InterestRate ?? 0,
                    InterestType = interest?.InterestRateType ?? ""
                };
            }).ToList();

            // ── Growth data: last 6 months ──
            var sixMonthsAgo = today.AddMonths(-6);
            var growthData = new System.Collections.Generic.List<ChartDataDto>();
            for (int i = 5; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                var mStart = new DateTime(month.Year, month.Month, 1);
                var mEnd = mStart.AddMonths(1);

                var fdsInMonth = activeFDs.Where(f => f.CreatedDate < mEnd).ToList();
                var count = fdsInMonth.Count;
                var value = fdsInMonth.Sum(f => f.PrincipalAmount);

                growthData.Add(new ChartDataDto
                {
                    Label = mStart.ToString("MMM yyyy"),
                    Value = value,
                    Count = count
                });
            }

            // ── Portfolio distribution by entity ──
            var entityGroups = activeFDs
                .GroupBy(f => new { f.EntityId, EntityName = f.Entity?.EntityName ?? $"Entity {f.EntityId}" })
                .Select(g => new ChartDataDto
                {
                    Label = g.Key.EntityName,
                    Value = g.Sum(f => f.PrincipalAmount),
                    Count = g.Count()
                })
                .ToList();

            return new DashboardSummaryDto
            {
                ActiveFDCount = activeFDCount,
                TotalPrincipal = totalPrincipal,
                TotalAccruedInterest = totalAccruedInterest,
                MaturingThisMonthCount = maturingThisMonthCount,
                MaturingThisMonthValue = maturingThisMonthValue,
                FDGrowthData = growthData,
                PortfolioDistributionData = entityGroups,
                UpcomingMaturities = upcomingMaturities,
                RecentlyAddedFDs = recentlyAdded
            };
        }
    }
}
