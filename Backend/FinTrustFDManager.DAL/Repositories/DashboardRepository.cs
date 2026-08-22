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
            var today = DateTime.Today; // Use local midnight date for business dates

            // ---------------------------------------------------------
            // 1. Total Active FDs and Principal
            // ---------------------------------------------------------
            var activeFdsQuery = _context.FDIdentifications
                .Where(x => x.Status == "Active");

            summary.ActiveFDCount = await activeFdsQuery.CountAsync();
            summary.TotalPrincipal = await activeFdsQuery.SumAsync(x => (decimal?)x.PrincipalAmount) ?? 0m;

            // ---------------------------------------------------------
            // 2. Total Accrued Interest
            // ---------------------------------------------------------
            // Exclude "Compounding Interest" events to prevent double counting.
            // We join with active FDs to ensure we only sum accrued interest for currently active investments.
            var accruedInterest = await _context.FDCashFlows
                .Where(c => c.Event == "Interest" && c.EndDate <= today)
                .Join(activeFdsQuery,
                      cf => cf.FdId,
                      fd => fd.FdId,
                      (cf, fd) => cf)
                .SumAsync(c => (decimal?)c.InterestAmount) ?? 0m;

            summary.TotalAccruedInterest = accruedInterest;

            // ---------------------------------------------------------
            // 3. Maturing This Month
            // ---------------------------------------------------------
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            var maturingThisMonthFds = await activeFdsQuery
                .Where(x => x.EndDate >= startOfMonth && x.EndDate < startOfNextMonth)
                .AsNoTracking()
                .ToListAsync();

            summary.MaturingThisMonthCount = maturingThisMonthFds.Count;

            if (maturingThisMonthFds.Any())
            {
                var maturingFdIds = maturingThisMonthFds.Select(x => x.FdId).ToList();

                var maturityFlows = await _context.FDCashFlows
                    .Where(c => c.Event == "Maturity" && maturingFdIds.Contains(c.FdId))
                    .AsNoTracking()
                    .ToListAsync();

                // Sum actual maturity flow if generated, otherwise fallback to PrincipalAmount
                summary.MaturingThisMonthValue = maturingThisMonthFds.Sum(fd =>
                {
                    var flow = maturityFlows.FirstOrDefault(f => f.FdId == fd.FdId);
                    return flow != null ? flow.CashFlowAmount : fd.PrincipalAmount;
                });
            }

            // ---------------------------------------------------------
            // 4. FD Growth Data (Last 6 Months)
            // ---------------------------------------------------------
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => new DateTime(today.Year, today.Month, 1).AddMonths(-5 + i))
                .ToList();

            foreach (var month in last6Months)
            {
                var monthStart = month;
                var nextMonthStart = month.AddMonths(1);

                // Note: We deliberately do NOT filter by 'Active' status here.
                // We want historical volume created in that month.
                var createdCount = await _context.FDIdentifications
                    .Where(x => x.CreatedDate >= monthStart && x.CreatedDate < nextMonthStart)
                    .CountAsync();

                var createdValue = await _context.FDIdentifications
                    .Where(x => x.CreatedDate >= monthStart && x.CreatedDate < nextMonthStart)
                    .SumAsync(x => (decimal?)x.PrincipalAmount) ?? 0m;

                summary.FDGrowthData.Add(new ChartDataDto
                {
                    Label = month.ToString("MMM yyyy"),
                    Count = createdCount,
                    Value = createdValue
                });
            }

            // ---------------------------------------------------------
            // 5. Portfolio Distribution Data (Group by Bank/Entity)
            // ---------------------------------------------------------
            var portfolio = await activeFdsQuery
                .GroupBy(x => x.EntityId)
                .Select(g => new
                {
                    EntityId = g.Key,
                    Count = g.Count(),
                    TotalPrincipal = g.Sum(x => x.PrincipalAmount)
                })
                .ToListAsync();

            if (portfolio.Any())
            {
                // EntityId in FDIdentification is long, but Entity.EntityId is int. Safe cast to int.
                var entityIds = portfolio.Select(p => (int)p.EntityId).ToList();
                var entities = await _context.Entities
                    .Where(e => entityIds.Contains(e.EntityId))
                    .ToDictionaryAsync(e => e.EntityId, e => e.EntityName);

                foreach (var item in portfolio)
                {
                    summary.PortfolioDistributionData.Add(new ChartDataDto
                    {
                        Label = entities.TryGetValue((int)item.EntityId, out var name) ? name : "Unknown",
                        Count = item.Count,
                        Value = item.TotalPrincipal
                    });
                }
            }

            // ---------------------------------------------------------
            // 6. Upcoming Maturities (Next 30 Days)
            // ---------------------------------------------------------
            var limit30Days = today.AddDays(31); // Exclusive upper bound for 30 days
            var upcomingMaturities = await activeFdsQuery
                .Where(x => x.EndDate >= today && x.EndDate < limit30Days)
                .OrderBy(x => x.EndDate)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            if (upcomingMaturities.Any())
            {
                var upcomingFdIds = upcomingMaturities.Select(x => x.FdId).ToList();
                
                var upcomingMaturityFlows = await _context.FDCashFlows
                    .Where(c => c.Event == "Maturity" && upcomingFdIds.Contains(c.FdId))
                    .ToDictionaryAsync(c => c.FdId, c => c.CashFlowAmount);

                var upcomingEntityIds = upcomingMaturities.Select(x => (int)x.EntityId).Distinct().ToList();
                var upcomingEntities = await _context.Entities
                    .Where(e => upcomingEntityIds.Contains(e.EntityId))
                    .ToDictionaryAsync(e => e.EntityId, e => e.EntityName);

                foreach (var fd in upcomingMaturities)
                {
                    summary.UpcomingMaturities.Add(new FDUpcomingMaturityDto
                    {
                        FdId = fd.FdId,
                        FdReferenceNo = fd.FdReferenceNo ?? "N/A",
                        BankName = upcomingEntities.TryGetValue((int)fd.EntityId, out var name) ? name : "Unknown",
                        PrincipalAmount = fd.PrincipalAmount,
                        MaturityDate = fd.EndDate,
                        // Safely fallback to Principal if Maturity flow is not generated yet
                        MaturityAmount = upcomingMaturityFlows.TryGetValue(fd.FdId, out var amount) ? amount : fd.PrincipalAmount,
                        Status = GetMaturityStatus(fd.EndDate, today)
                    });
                }
            }

            // ---------------------------------------------------------
            // 7. Recently Added FDs
            // ---------------------------------------------------------
            var recentFds = await _context.FDIdentifications
                .OrderByDescending(x => x.CreatedDate)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            if (recentFds.Any())
            {
                var recentFdIds = recentFds.Select(x => x.FdId).ToList();
                
                var recentInterests = await _context.FDInterests
                    .Where(i => recentFdIds.Contains(i.FdId))
                    .ToDictionaryAsync(i => i.FdId, i => i);

                foreach (var fd in recentFds)
                {
                    recentInterests.TryGetValue(fd.FdId, out var interest);
                    
                    summary.RecentlyAddedFDs.Add(new FDRecentDto
                    {
                        FdId = fd.FdId,
                        FdReferenceNo = fd.FdReferenceNo ?? "N/A",
                        StartDate = fd.StartDate,
                        PrincipalAmount = fd.PrincipalAmount,
                        InterestRate = interest?.InterestRate ?? 0,
                        InterestType = (interest?.IsCompounding ?? false) ? "Cumulative" : "Non-Cumulative"
                    });
                }
            }

            return summary;
        }

        private string GetMaturityStatus(DateTime endDate, DateTime currentDate)
        {
            var days = (int)(endDate.Date - currentDate.Date).TotalDays;
            
            if (days < 0) return "Matured";
            if (days == 0) return "Due Today";
            
            return $"Due in {days} Days";
        }
    }
}
