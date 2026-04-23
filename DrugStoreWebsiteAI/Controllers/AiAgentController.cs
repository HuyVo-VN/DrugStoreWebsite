using DrugStoreWebsiteAI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DrugStoreWebsiteAI.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class AiAgentController : ControllerBase
    {
        private readonly IExcelParserService _excelParser;
        private readonly IAiAgentService _aiService;

        public AiAgentController(IExcelParserService excelParser, IAiAgentService aiService)
        {
            _excelParser = excelParser;
            _aiService = aiService;
        }

        public class ChatRequest
        {
            public string message { get; set; } = string.Empty;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.message))
                return BadRequest(new { status = "error", message = "Tin nhắn trống!" });

            try
            {
                // Gọi Não Giao Tiếp
                var replyMessage = await _aiService.ChatAsync(req.message);

                // Trả về đúng format mà file admin-chatbox.ts của sếp đang mong đợi
                return Ok(new
                {
                    status = "success",
                    reply = replyMessage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", reply = "Hệ thống AI đang bảo trì: " + ex.Message });
            }
        }

        [HttpPost("upload-excel")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Không tìm thấy file tệp tin!" });

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest(new { message = "Chỉ chấp nhận định dạng Excel (.xlsx, .xls)!" });

            try
            {
                // 1. Dùng "Đôi mắt" bóc tách Excel thành JSON
                var rawData = await _excelParser.ParseExcelDynamicAsync(file);

                // --- TRẠM DỪNG CHÂN ---
                // Hiện tại ta chỉ trả về JSON để sếp test. 
                // Ở bước tiếp theo, ta sẽ ném biến 'rawData' này vào cho Semantic Kernel đọc!

                return Ok(new
                {
                    status = 200,
                    message = "Bóc tách Excel thành công!",
                    totalRows = rawData.Count,
                    dataPreview = rawData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi đọc file: " + ex.Message });
            }
        }

        [HttpPost("process-inventory")]
        public async Task<IActionResult> ProcessInventory(IFormFile file, [FromForm] string connectionId = "")
        {
            if (file == null || file.Length == 0) return BadRequest("File không hợp lệ.");

            try
            {
                // 1. Bóc tách Excel thành JSON thô
                var rawData = await _excelParser.ParseExcelDynamicAsync(file);

                // 2. Đưa cho AI xử lý thông minh
                var aiResultJson = await _aiService.ProcessRawDataWithAiAsync(rawData, connectionId);

                // Trả về kết quả đã được AI bóc tách
                return Ok(new
                {
                    status = 200,
                    message = "AI đã xử lý dữ liệu thành công!",
                    data = JsonSerializer.Deserialize<object>(aiResultJson) // 👈 HẾT LỖI TẠI ĐÂY
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }
    }
}