using System.Threading.Tasks;
using FinTrustFDManager.Model.DTOs.Dashboard;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardSummaryDto> GetSummaryAsync();
    }
}
