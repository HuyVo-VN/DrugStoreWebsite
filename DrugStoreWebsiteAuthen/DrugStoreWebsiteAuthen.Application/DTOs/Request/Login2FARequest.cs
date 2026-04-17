using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrugStoreWebsiteAuthen.Application.DTOs.Request
{
    public class Login2FARequest
    {
        public string Username { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // Mã 6 số từ Google Authenticator
    }
}
