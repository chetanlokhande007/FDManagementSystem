using System.Threading.Tasks;
using FinTrustFDManager.Model.DTOs.Dashboard;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummaryAsync();
    }
}
