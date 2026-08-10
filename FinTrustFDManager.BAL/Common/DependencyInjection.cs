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

            return services;
        }

        public static IServiceCollection AddDAL(
            this IServiceCollection services)
        {
            services.AddScoped<IEntityRepository, EntityRepository>();

            return services;
        }
    }
}
