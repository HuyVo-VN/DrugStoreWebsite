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
                return BadRequest(new { status = "error", message = "Empty message!" });

            try
            {
                var replyMessage = await _aiService.ChatAsync(req.message);

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
        public async Task<IActionResult> ProcessInventory(IFormFile file, [FromForm] string connectionId = "")
        {
            if (file == null || file.Length == 0) return BadRequest("Invalid file.");

            try
            {
                // excute Excel to RAW JSON
                var rawData = await _excelParser.ParseExcelDynamicAsync(file);

                var aiResultJson = await _aiService.ProcessRawDataWithAiAsync(rawData, connectionId);

                return Ok(new
                {
                    status = 200,
                    message = "The AI ​​processed the data successfully!",
                    data = JsonSerializer.Deserialize<object>(aiResultJson)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"System error: {ex.Message}");
            }
        }
    }
}