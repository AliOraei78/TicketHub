using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Application.DTOs;

public class FieldTypeDto
{
    public int Id { get; set; } //[cite: 1]
    public string Type { get; set; } = string.Empty; //[cite: 1]
    public bool IsActive { get; set; } //[cite: 1]
}
