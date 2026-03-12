using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrugStoreWebSiteData.Application.DTOs.Request
{
    public class PaymentRequestDto
    {
        public Guid OrderId { get; set; }

        public double Amount { get; set; }

        public string OrderDescription { get; set; } = string.Empty;
    }
}
