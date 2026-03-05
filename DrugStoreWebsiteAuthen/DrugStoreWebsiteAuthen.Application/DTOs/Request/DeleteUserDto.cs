using System;
using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebsiteAuthen.Application.DTOs.Request;

public class DeleteUserDto
{
    [Required]
    [StringLength(50, MinimumLength = 5)]
    public string UserId { get; set; }
}
