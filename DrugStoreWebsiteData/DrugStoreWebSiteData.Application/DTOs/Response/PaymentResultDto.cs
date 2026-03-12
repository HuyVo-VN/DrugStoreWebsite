using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrugStoreWebSiteData.Application.DTOs.Response
{
    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public Guid OrderId { get; set; }
    }
}
