using System;
using System.Collections.Generic;
using TicketHub.Core.Entities;

namespace TicketHub.Application.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsConfirmed { get; set; }
    public DateTime? TokenExpiration { get; set; }
    public List<string> RoleNames { get; set; } = new();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}