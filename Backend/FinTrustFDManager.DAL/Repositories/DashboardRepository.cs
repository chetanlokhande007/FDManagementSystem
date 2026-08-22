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
                .Where(f => f.Status == "APPROVED" || f.Status == "DRAFT")
                .ToListAsync();

            var activeFDCount = activeFDs.Count;
            var totalPrincipal = activeFDs.Sum(f => f.PrincipalAmount);

            // ── Accrued interest: sum of Interest events from FDCashFlows ──
            var totalAccruedInterest = await _context.FDCashFlows
                .AsNoTracking()
                .Where(cf => cf.Event == "Interest" || cf.Event == "Compounding Interest")
                .SumAsync(cf => cf.InterestAmount);

            // ── Maturity amounts per FD (last Maturity cash flow) ──
            var maturityAmounts = await _context.FDCashFlows
                .AsNoTracking()
                .Where(cf => cf.Event == "Maturity")
                .GroupBy(cf => cf.FdId)
                .Select(g => new { FdId = g.Key, MaturityAmount = g.Sum(cf => cf.CashFlowAmount) })
                .ToDictionaryAsync(x => x.FdId, x => x.MaturityAmount);

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
                    BankName = "",  // FDIdentification has no direct Bank FK
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

            var recentlyAdded = recentFDs.Select(f =>
            {
                var interest = _context.FDInterests
                    .AsNoTracking()
                    .FirstOrDefault(i => i.FdId == f.FdId);

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
                .GroupBy(f => f.EntityId)
                .Select(g => new ChartDataDto
                {
                    Label = $"Entity {g.Key}",
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
