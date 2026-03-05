using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebsiteAuthen.Application.DTOs.Request;
public class ForgetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
