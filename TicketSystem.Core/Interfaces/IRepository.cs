// TicketHub.Core/Interfaces/IRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TicketHub.Core.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }
}