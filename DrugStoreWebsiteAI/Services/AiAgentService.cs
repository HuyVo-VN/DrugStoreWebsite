using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using System.Text.Json;
using DrugStoreWebsiteAI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel.ChatCompletion;
using DrugStoreWebsiteAI.Controllers;

namespace DrugStoreWebsiteAI.Services
{
    public interface IAiAgentService
    {
        Task<string> ProcessRawDataWithAiAsync(List<Dictionary<string, string>> rawData);
        Task<string> ProcessRawDataWithAiAsync(List<Dictionary<string, string>> rawData, string connectionId);
        Task<string> ChatAsync(List<AiAgentController.ChatMessageDto> messages);
    }

    public class AiAgentService : IAiAgentService
    {
        private readonly Kernel _kernel;
        private readonly IHubContext<AiHub> _hubContext;

        public AiAgentService(Kernel kernel, IHubContext<AiHub> hubContext)
        {
            _kernel = kernel;
            _hubContext = hubContext;
        }

        public async Task<string> ProcessRawDataWithAiAsync(List<Dictionary<string, string>> rawData)
        {
            // Chuyển mảng Dictionary thành chuỗi JSON để gửi cho AI
            var jsonInput = JsonSerializer.Serialize(rawData);

            // Đây là "Câu lệnh thần thánh" (Prompt) để dạy Gemini cách phân loại dữ liệu
            var prompt = $@"
            You are a specialist in extracting standardized pharmaceutical data.
            I will give you a list of raw JSON extracted from Excel.
            Your task is to analyze the semantics of the 'Keys' (column names) and convert it to the following standard structure:
            1. 'BaseInfo': Contains the basic fields (Name, Price, Quantity, Category, Status, Description).
            2. 'SaleInfo': Contains fields related to discounts, promotions, and sales percentages.
            3. 'Specifications': All remaining columns (Ingredients, Indications, Usage, Expiration Date, Manufacturer, etc.) should be grouped here as Key-Values.
            4. 'Images': If any column contains image links or image file names, include them in this array.
            Requirement: Return only the resulting JSON string, without any further explanation.
            INPUT DATA:
            {jsonInput}";

            // Gửi yêu cầu cho Gemini
            var result = await _kernel.InvokePromptAsync(prompt);
            var responseText = result.ToString() ?? "[]";

            responseText = responseText.Replace("```json", "")
                                       .Replace("```", "")
                                       .Trim();

            return responseText;
        }

        public async Task<string> ProcessRawDataWithAiAsync(List<Dictionary<string, string>> rawData, string connectionId)
        {
            // 1. KIỂM TRA BẢO VỆ
            if (rawData == null || !rawData.Any()) return "[]";

            await SendProgress(connectionId, "Extracting column headers to send to AI (Super Savings Token)...");

            // 2. CHỈ LẤY TIÊU ĐỀ CỘT (Headers) - Trọng lượng siêu nhẹ!
            var headers = rawData.First().Keys.ToList();
            var headersJson = JsonSerializer.Serialize(headers);

            // 3. PROMPT HOA TIÊU (Chỉ bắt AI tạo Rule Mapping)
            var prompt = $@"
            You are a pharmaceutical data analyst. 
            I have a list of column names from an Excel file as follows: {headersJson}

            Create a normalized mapping rule in a single JSON format.
            Required JSON structure to return:
            {{
                ""BaseInfo"": [""Column code Drug name"", ""Column code Price"", ""Column code Quantity"", ""Column code Category""],
                ""SaleInfo"": [""Column code Sale"", ""Column code Promo""],
                ""Images"": [""Column code link picture""],
                ""Specifications"": [""All remaining columns (Ingredients, Instructions for Use, Manufacturer, etc.)""]
            }}
            It only returns JSON, without explanation. The original column names in the list I provide must be preserved.";

            await SendProgress(connectionId, "AI is mapping rules...");

            // Gọi AI (Tốn chưa tới 100 Token)
            var result = await _kernel.InvokePromptAsync(prompt);
            var mappingRuleStr = result.ToString().Replace("```json", "").Replace("```", "").Trim();

            await SendProgress(connectionId, "We already have maps from AI! C# is using its muscles to process thousands of Excel rows...");

            try
            {
                using var jsonDoc = JsonDocument.Parse(mappingRuleStr);
                var root = jsonDoc.RootElement;

                var standardizedDataList = new List<object>();

                foreach (var row in rawData)
                {
                    var baseInfo = new Dictionary<string, string>();
                    var saleInfo = new Dictionary<string, string>();
                    var specs = new Dictionary<string, string>();
                    var images = new List<string>();

                    foreach (var kvp in row)
                    {
                        var colName = kvp.Key;
                        var colValue = kvp.Value;

                        if (IsColumnInArray(root, "BaseInfo", colName)) baseInfo[colName] = colValue;
                        else if (IsColumnInArray(root, "SaleInfo", colName)) saleInfo[colName] = colValue;
                        else if (IsColumnInArray(root, "Images", colName)) images.Add(colValue);
                        else specs[colName] = colValue;
                    }

                    standardizedDataList.Add(new
                    {
                        BaseInfo = baseInfo,
                        SaleInfo = saleInfo,
                        Specifications = specs,
                        Images = images
                    });
                }

                await SendProgress(connectionId, "Processing complete! Return the results to the interface.");
                return JsonSerializer.Serialize(standardizedDataList);
            }
            catch (Exception ex)
            {
                return $"[{{\"Error\": \"Error when applying AI maps: {ex.Message}\"}}]";
            }
        }

        // Hàm phụ trợ giúp C# đọc JSON của AI
        private bool IsColumnInArray(JsonElement root, string arrayName, string columnName)
        {
            if (root.TryGetProperty(arrayName, out var array))
            {
                foreach (var item in array.EnumerateArray())
                {
                    if (item.GetString() == columnName) return true;
                }
            }
            return false;
        }

        // Hàm phụ trợ để gửi tin nhắn cho gọn code
        private async Task SendProgress(string connectionId, string message)
        {
            if (!string.IsNullOrEmpty(connectionId))
            {
                // Gọi hàm 'ReceiveAiProgress' ở dưới Frontend (Angular)
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveAiProgress", message);
            }
        }

        public async Task<string> ChatAsync(List<AiAgentController.ChatMessageDto> messages)
        {
            try
            {
                var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

                var chatHistory = new ChatHistory($@"
                You are the Super Admin AI Agent for a DrugStore ecosystem.
                DECISION MATRIX:
                1. GREETINGS: Answer briefly in English.
                2. QUERIES: Call 'get_database_schema' -> 'execute_readonly_sql_query'.
                3. EXPORT FILE: Call 'export_data_to_excel'.
                4. IMPORT EXCEL: If Admin says 'Yes', 'Ok', 'Enter warehouse', 'Save' Or words or phrases that are something like ""I agree"". After uploading the file, find the session key (cacheKey) in the previous conversation and call the function. 'import_approved_inventory_data'.
                ");

                foreach (var msg in messages)
                {
                    if (msg.Role == "user") chatHistory.AddUserMessage(msg.Content);
                    else if (msg.Role == "ai") chatHistory.AddAssistantMessage(msg.Content);
                }

                var executionSettings = new PromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
                var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings, _kernel);

                return result.Content ?? "The AI system did not return any results.";
            }
            catch (Exception ex)
            {
                return $"❌ **A system error has occurred.:** {ex.Message}";
            }
        }
    }
}