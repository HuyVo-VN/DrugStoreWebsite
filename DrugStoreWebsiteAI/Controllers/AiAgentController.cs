using DrugStoreWebsiteAI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace DrugStoreWebsiteAI.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class AiAgentController : ControllerBase
    {
        private readonly IExcelParserService _excelParser;
        private readonly IAiAgentService _aiService;
        private readonly IDistributedCache _cache;

        public AiAgentController(IExcelParserService excelParser, IAiAgentService aiService, IDistributedCache cache)
        {
            _excelParser = excelParser;
            _aiService = aiService;
            _cache = cache;
        }
        public class ChatMessageDto
        {
            public string Role { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }

        public class ChatRequest
        {
            public List<ChatMessageDto> Messages { get; set; } = new();
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest req)
        {
            if (req.Messages == null || !req.Messages.Any())
                return BadRequest(new { status = "error", message = "Empty message!" });

            try
            {
                var replyMessage = await _aiService.ChatAsync(req.Messages);

                return Ok(new
                {
                    status = "success",
                    reply = replyMessage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", reply = "The AI ​​system is undergoing maintenance:" + ex.Message });
            }
        }

        [HttpPost("upload-excel")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "File not found!" });

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest(new { message = "Only Excel format is accepted. (.xlsx, .xls)!" });

            try
            {
                // Excute Excel to JSON
                var rawData = await _excelParser.ParseExcelDynamicAsync(file);

                return Ok(new
                {
                    status = 200,
                    message = "Excel data extraction successful!",
                    totalRows = rawData.Count,
                    dataPreview = rawData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error while reading file: " + ex.Message });
            }
        }

        [HttpPost("process-inventory")]
        public async Task<IActionResult> ProcessInventory(IFormFile file, [FromForm] string connectionId = "", [FromForm] string userMessage = "")
        {
            if (file == null || file.Length == 0) return BadRequest("Invalid file.");

            try
            {
                var rawData = await _excelParser.ParseExcelDynamicAsync(file);

                // Kiểm tra an toàn nếu file Excel rỗng
                if (rawData == null || !rawData.Any())
                    return BadRequest(new { status = "error", reply = "The Excel file contains no data or is incorrectly formatted." });

                var aiResultJson = await _aiService.ProcessRawDataWithAiAsync(rawData, connectionId);

                // Lưu JSON vào Redis
                var cacheKey = $"excel_import_{Guid.NewGuid()}";
                await _cache.SetStringAsync(cacheKey, aiResultJson, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });

                // Lấy danh sách tên cột từ dòng đầu tiên của Excel
                var headersList = rawData.First().Keys.ToList();

                // Nhờ AI phân tích xem thiếu đủ cột gì, kết hợp với lời dặn để tự tạo câu trả lời
                string aiReply = await _aiService.AnalyzeHeadersAsync(headersList, cacheKey, userMessage);

                return Ok(new
                {
                    status = 200,
                    reply = aiReply,
                    data = JsonSerializer.Deserialize<object>(aiResultJson)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n\n[ERROR 500 PROCESS INVENTORY]: {ex.ToString()}\n\n");
                return StatusCode(500, $"System error: {ex.Message}");
            }
        }
    }
}