using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DrugStoreWebsiteAuthen.Domain
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ImageUrl { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
 
}
