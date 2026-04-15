using Azure.Core;
using DrugStoreWebSiteData.Application.DTOs;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Application.Service;
using DrugStoreWebSiteData.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrugStoreWebSiteData.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IVnPayService _vnPayService;
        private readonly IOrderService _orderService;

        public PaymentsController(IVnPayService vnPayService, IOrderService orderService)
        {
            _vnPayService = vnPayService;
            _orderService = orderService;
        }

        [HttpPost("create-payment-url")]
        public IActionResult CreatePaymentUrl([FromBody] PaymentRequestDto request)
        {
            try
            {
                var url = _vnPayService.CreatePaymentUrl(HttpContext, request);

                return Ok(new { status = 200, url = url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = 400, message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("payment-callback")]
        public async Task<IActionResult> PaymentCallback()
        {
            try
            {
                // Request.Query sẽ tự động lấy toàn bộ tham số trên thanh địa chỉ (URL)
                var response = _vnPayService.PaymentExecute(Request.Query);

                if (response.Success)
                {
                    // GỌI HÀM CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG CỦA BẠN (Tùy theo enum của bạn là Paid hay Completed)
                    // Theo API OrderController của bạn, ta dùng UpdateStatusAsync:
                    var updateRequest = new UpdateOrderStatusRequestDto
                    {
                        OrderId = response.OrderId,
                        NewStatus = DrugStoreWebSiteData.Domain.Enums.OrderStatus.Paid // Hãy đảm bảo bạn có trạng thái Paid (Đã thanh toán)
                    };

                    await _orderService.UpdateStatusAsync(updateRequest);

                    return Ok(new { status = 200, message = "Payment successful", orderId = response.OrderId });
                }

                return BadRequest(new { status = 400, message = "Payment failed or was canceled." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = 400, message = ex.Message });
            }
        }
    }

}
