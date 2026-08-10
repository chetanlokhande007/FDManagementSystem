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
            services.AddScoped<IEntityService, EntityService>();

            services.AddScoped<ICurrencyService,
                CurrencyService>();

            services.AddScoped<ICountryService,
                CountryService>();

            services.AddScoped<ICounterPartyService,
                CounterPartyService>();

            services.AddScoped<IBankService,
                BankService>();

            services.AddScoped<IBankAccountService,
                BankAccountService>();

            return services;
        }

        public static IServiceCollection AddDAL(
            this IServiceCollection services)
        {
            services.AddScoped<IEntityRepository, EntityRepository>();

            services.AddScoped<ICurrencyRepository,
                CurrencyRepository>();

            services.AddScoped<ICountryRepository,
                CountryRepository>();

            services.AddScoped<ICounterPartyRepository,
                CounterPartyRepository>();

            services.AddScoped<IBankRepository,
                BankRepository>();

            services.AddScoped<IBankAccountRepository,
                BankAccountRepository>();

            return services;
        }
    }
}
