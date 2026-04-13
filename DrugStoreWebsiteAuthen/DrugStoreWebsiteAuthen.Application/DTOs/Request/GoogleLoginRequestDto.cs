using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrugStoreWebsiteAuthen.Application.DTOs.Request
{
    public class GoogleLoginRequestDto
    {
        public string IdToken { get; set; } = string.Empty;
    }
}
