using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;
using Microsoft.AspNetCore.Http;

namespace DrugStoreWebSiteData.Application.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, PaymentRequestDto model);
        PaymentResultDto PaymentExecute(IQueryCollection collections);
    }
}
