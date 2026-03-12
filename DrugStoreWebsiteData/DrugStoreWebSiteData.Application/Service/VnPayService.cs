using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.VnPay;
using DrugStoreWebSiteData.Application.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using DrugStoreWebSiteData.Application.DTOs.Response;
namespace DrugStoreWebSiteData.Application.Service
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPayConfig _config;

        public VnPayService(IOptions<VnPayConfig> options)
        {
            _config = options.Value;
        }

        public string CreatePaymentUrl(HttpContext context, PaymentRequestDto model)
        {
            var tick = DateTime.Now.Ticks.ToString();
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", _config.Version);
            vnpay.AddRequestData("vnp_Command", _config.Command);
            vnpay.AddRequestData("vnp_TmnCode", _config.TmnCode);

            // VNPay quy định số tiền gửi đi phải nhân với 100 (VD: 100,000 VND -> gửi 10000000)
            // Vì hệ thống của bạn dùng USD, ta quy đổi tượng trưng 1 USD = 24000 VND để VNPay hiểu
            var amountInVnd = (long)(model.Amount * 24000 * 100);
            vnpay.AddRequestData("vnp_Amount", amountInVnd.ToString());

            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", _config.CurrCode);
            vnpay.AddRequestData("vnp_IpAddr", VnPayLibrary.GetIpAddress(context));
            vnpay.AddRequestData("vnp_Locale", _config.Locale);

            // Chỗ này truyền cái OrderId vào để lúc VNPay trả về, ta biết đơn nào vừa thanh toán xong
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {model.OrderId}");
            vnpay.AddRequestData("vnp_OrderType", "other"); // Loại hàng hóa
            vnpay.AddRequestData("vnp_ReturnUrl", _config.ReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", model.OrderId.ToString()); // Mã tham chiếu giao dịch (phải duy nhất)

            var paymentUrl = vnpay.CreateRequestUrl(_config.BaseUrl, _config.HashSecret);

            return paymentUrl;
        }

        public PaymentResultDto PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();

            // Đổ toàn bộ tham số từ URL vào thư viện
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            var vnp_orderId = vnpay.GetResponseData("vnp_TxnRef");
            var vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");

            // Xác thực chữ ký xem có bị Hacker sửa giá tiền trên URL không
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _config.HashSecret);

            if (!checkSignature) return new PaymentResultDto { Success = false };

            return new PaymentResultDto
            {
                // Mã 00 nghĩa là khách đã quẹt thẻ thành công
                Success = vnp_ResponseCode == "00",
                OrderId = Guid.Parse(vnp_orderId)
            };
        }
    }
}
