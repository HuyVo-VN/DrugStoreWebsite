using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;

namespace DrugStoreWebsiteAuthen.Application.DTOs.Request;
public class ResetPasswordRequest
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
    [Required, DataType(DataType.Password)]
    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; set; }

}
