using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<int> SaveChangesAsync();
    }
}
