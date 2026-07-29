using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketHub.Core.Entities;

namespace TicketHub.Application.Interfaces;

public interface IPriorityService
{
    Task<IEnumerable<Priority>> GetAllAsync();
    Task AddAsync(Priority priority);
    Task UpdateAsync(Priority priority);
    Task DeleteAsync(int id);
    Task DeleteRangeAsync(IEnumerable<int> ids);
}
