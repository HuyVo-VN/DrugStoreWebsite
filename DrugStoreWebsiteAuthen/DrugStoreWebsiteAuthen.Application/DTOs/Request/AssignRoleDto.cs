using System;
using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebsiteAuthen.Application.DTOs.Request;

public class AssignRoleDto
{
    [Required]
    public string UserId { get; set; }
    [Required]
    public string RoleName { get; set; }

}
