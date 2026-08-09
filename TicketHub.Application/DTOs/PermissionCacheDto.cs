using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TicketHub.Application.Enums;

namespace TicketHub.Application.DTOs;

public class PermissionCacheDto
{
    public string ResourceKey { get; set; } = string.Empty;
    public PermissionType Type { get; set; }
    public List<string> AllowedRoles { get; set; } = new();
}
