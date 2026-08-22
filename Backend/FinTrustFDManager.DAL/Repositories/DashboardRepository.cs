using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            var summary = new DashboardSummaryDto();
            var currentDate = DateTime.UtcNow;

            // 1. Total Active FDs and Principal
            var activeFds = await _context.FDIdentifications
                .Where(x => x.Status == "Active")
                .AsNoTracking()
                .ToListAsync();

            summary.ActiveFDCount = activeFds.Count;
            summary.TotalPrincipal = activeFds.Sum(x => x.PrincipalAmount);

            // 2. Total Accrued Interest 
            // We sum the InterestAmount for CashFlows of Active FDs up to the current date
            // The user explicitly stated: Accrued interest should not be double-counted as cash movement.
            // We just need the accrued amount.
            var accruedInterest = await _context.FDCashFlows
                .Include(c => c.FdId) // To filter by Active FDs
                .Where(c => c.Event == "Interest" || c.Event == "Compounding Interest") // Interest events
                .Join(_context.FDIdentifications.Where(fd => fd.Status == "Active"),
                      cf => cf.FdId,
                      fd => fd.FdId,
                      (cf, fd) => cf)
                .Where(c => c.EndDate <= currentDate)
                .SumAsync(c => c.InterestAmount);

            summary.TotalAccruedInterest = accruedInterest;

            // 3. Maturing This Month
            var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var maturingThisMonthFds = activeFds
                .Where(x => x.EndDate >= startOfMonth && x.EndDate <= endOfMonth)
                .ToList();

            summary.MaturingThisMonthCount = maturingThisMonthFds.Count;
            
            // To get Maturity Amount, we need to look at Maturity cashflows
            var maturingFdIds = maturingThisMonthFds.Select(x => x.FdId).ToList();
            var maturityFlows = await _context.FDCashFlows
                .Where(c => maturingFdIds.Contains(c.FdId) && c.Event == "Maturity")
                .AsNoTracking()
                .ToListAsync();
                
            summary.MaturingThisMonthValue = maturityFlows.Sum(x => x.CashFlowAmount);

            // 4. FD Growth Data (Last 6 Months)
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(-5 + i))
                .ToList();

            foreach (var month in last6Months)
            {
                var monthStart = month;
                var monthEnd = month.AddMonths(1).AddDays(-1);

                var createdInMonth = activeFds
                    .Where(x => x.CreatedDate >= monthStart && x.CreatedDate <= monthEnd)
                    .ToList();

                summary.FDGrowthData.Add(new ChartDataDto
                {
                    Label = month.ToString("MMM yyyy"),
                    Count = createdInMonth.Count,
                    Value = createdInMonth.Sum(x => x.PrincipalAmount)
                });
            }

            // 5. Portfolio Distribution Data (Group by Bank/Entity)
            var portfolio = await _context.FDIdentifications
                .Where(x => x.Status == "Active")
                .GroupBy(x => x.EntityId)
                .Select(g => new { EntityId = g.Key, Count = g.Count(), TotalPrincipal = g.Sum(x => x.PrincipalAmount) })
                .ToListAsync();

            // Fetch entity names
            var entityIds = portfolio.Select(p => (int)p.EntityId).ToList();
            var entities = await _context.Entities.Where(e => entityIds.Contains(e.EntityId)).ToDictionaryAsync(e => e.EntityId, e => e.EntityName);

            foreach (var item in portfolio)
            {
                summary.PortfolioDistributionData.Add(new ChartDataDto
                {
                    Label = entities.ContainsKey((int)item.EntityId) ? entities[(int)item.EntityId] : "Unknown",
                    Count = item.Count,
                    Value = item.TotalPrincipal
                });
            }

            // 6. Upcoming Maturities (Next 30 Days)
            var next30Days = currentDate.AddDays(30);
            var upcomingMaturities = await _context.FDIdentifications
                .Where(x => x.Status == "Active" && x.EndDate >= currentDate && x.EndDate <= next30Days)
                .OrderBy(x => x.EndDate)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            var upcomingFdIds = upcomingMaturities.Select(x => x.FdId).ToList();
            var upcomingMaturityFlows = await _context.FDCashFlows
                .Where(c => upcomingFdIds.Contains(c.FdId) && c.Event == "Maturity")
                .ToDictionaryAsync(c => c.FdId, c => c.CashFlowAmount);

            // Fetch banks for these upcoming FDs
            var upcomingEntityIds = upcomingMaturities.Select(x => (int)x.EntityId).Distinct().ToList();
            var upcomingEntities = await _context.Entities.Where(e => upcomingEntityIds.Contains(e.EntityId)).ToDictionaryAsync(e => e.EntityId, e => e.EntityName);

            foreach (var fd in upcomingMaturities)
            {
                summary.UpcomingMaturities.Add(new FDUpcomingMaturityDto
                {
                    FdId = fd.FdId,
                    FdReferenceNo = fd.FdReferenceNo,
                    BankName = upcomingEntities.ContainsKey((int)fd.EntityId) ? upcomingEntities[(int)fd.EntityId] : "Unknown",
                    PrincipalAmount = fd.PrincipalAmount,
                    MaturityDate = fd.EndDate,
                    MaturityAmount = upcomingMaturityFlows.ContainsKey(fd.FdId) ? upcomingMaturityFlows[fd.FdId] : fd.PrincipalAmount,
                    Status = GetMaturityStatus(fd.EndDate, currentDate)
                });
            }

            // 7. Recently Added FDs
            var recentFds = await _context.FDIdentifications
                .OrderByDescending(x => x.CreatedDate)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            var recentFdIds = recentFds.Select(x => x.FdId).ToList();
            var recentInterests = await _context.FDInterests
                .Where(i => recentFdIds.Contains(i.FdId))
                .ToDictionaryAsync(i => i.FdId, i => i);

            foreach (var fd in recentFds)
            {
                var interest = recentInterests.ContainsKey(fd.FdId) ? recentInterests[fd.FdId] : null;
                summary.RecentlyAddedFDs.Add(new FDRecentDto
                {
                    FdId = fd.FdId,
                    FdReferenceNo = fd.FdReferenceNo,
                    StartDate = fd.StartDate,
                    PrincipalAmount = fd.PrincipalAmount,
                    InterestRate = interest?.InterestRate ?? 0,
                    InterestType = (interest?.IsCompounding ?? false) ? "Cumulative" : "Non-Cumulative"
                });
            }

            return summary;
        }

        private string GetMaturityStatus(DateTime endDate, DateTime currentDate)
        {
            var days = (endDate - currentDate).TotalDays;
            if (days < 0) return "Matured";
            if (days <= 7) return $"Due in {(int)days} Days";
            if (days <= 15) return $"Due in {(int)days} Days";
            return $"Due in {(int)days} Days";
        }
    }
}
