using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketHub.Application.DTOs;
using TicketHub.Core.Entities;

namespace TicketHub.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<CategoryDto> AddAsync(CategoryDto category);
    Task UpdateAsync(CategoryDto category);
    Task DeleteAsync(CategoryDto category);
    Task DeleteRangeAsync(IEnumerable<int> ids);
}
