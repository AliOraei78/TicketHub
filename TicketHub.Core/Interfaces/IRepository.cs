// TicketHub.Core/Interfaces/IRepository.cs
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace TicketHub.Core.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();

        // Added method for eager loading related entities
        Task<IEnumerable<T>> GetAllWithIncludesAsync(params Expression<Func<T, object?>>[] includes);

        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }
}