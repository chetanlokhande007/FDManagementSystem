using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.BAL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrustFDManager.BAL.Common
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBAL(
            this IServiceCollection services)
        {
            // Master Data Services
            services.AddScoped<IEntityService, EntityService>();
            services.AddScoped<ICurrencyService, CurrencyService>();
            services.AddScoped<ICountryService, CountryService>();
            services.AddScoped<ICounterPartyService, CounterPartyService>();



            // Core Data Services
            services.AddScoped<IInterestFrequencyService, InterestFrequencyService>();
            services.AddScoped<IDayCountConventionService, DayCountConventionService>();
            services.AddScoped<IInvestmentService, InvestmentService>();
            services.AddScoped<ICashFlowService, CashFlowService>();
            services.AddScoped<IInvestmentApprovalService, InvestmentApprovalService>();

            // Auth Service
            services.AddScoped<IAuthService, AuthService>();

            // Dashboard Service
            services.AddScoped<IDashboardService, DashboardService>();

            return services;
        }

    }
}
