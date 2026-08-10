using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.BAL.Services;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.DAL.Repositories;
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
            services.AddScoped<IBankService, BankService>();
            services.AddScoped<IBankAccountService, BankAccountService>();

            // Core Data Services
            services.AddScoped<IInterestFrequencyService, InterestFrequencyService>();
            services.AddScoped<IDayCountConventionService, DayCountConventionService>();
            services.AddScoped<IInvestmentService, InvestmentService>();
            services.AddScoped<ICashFlowService, CashFlowService>();
            services.AddScoped<IInvestmentApprovalService, InvestmentApprovalService>();

            return services;
        }

        public static IServiceCollection AddDAL(
            this IServiceCollection services)
        {
            // Master Data Repositories
            services.AddScoped<IEntityRepository, EntityRepository>();
            services.AddScoped<ICurrencyRepository, CurrencyRepository>();
            services.AddScoped<ICountryRepository, CountryRepository>();
            services.AddScoped<ICounterPartyRepository, CounterPartyRepository>();
            services.AddScoped<IBankRepository, BankRepository>();
            services.AddScoped<IBankAccountRepository, BankAccountRepository>();

            // Core Data Repositories
            services.AddScoped<IInterestFrequencyRepository, InterestFrequencyRepository>();
            services.AddScoped<IDayCountConventionRepository, DayCountConventionRepository>();
            services.AddScoped<IInvestmentRepository, InvestmentRepository>();
            services.AddScoped<ICashFlowRepository, CashFlowRepository>();
            services.AddScoped<IInvestmentApprovalRepository, InvestmentApprovalRepository>();

            return services;
        }
    }
}
