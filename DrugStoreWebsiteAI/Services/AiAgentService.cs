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
        Task<string> AnalyzeHeadersAsync(List<string> headers, string cacheKey, string userMessage);
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
                4. [CRITICAL RULE FOR EXCEL IMPORT]:
                When you analyze an uploaded Excel file and find that a required field (like 'Category' or 'Description') is missing from the columns, DO NOT immediately ask the user for it.
                Step 1: Strictly examine the user's current chat prompt (e.g., 'I want to add these to Bone Joint Health category').
                Step 2: If the missing information is explicitly mentioned in the user's prompt, automatically extract it and apply it to the extracted data payload.
                Step 3: Only ask the user for missing fields if the information is absent in BOTH the file AND the chat prompt.

                5. PRE-IMPORT REVIEW & CONFIRMATION (MANDATORY):
                   - When you finish analyzing the Excel file and determine that the data is 100% VALID (no missing data, no duplicates, Category is identified):
                    + DO NOT call the import function yet.
                    + Present a brief, professional summary of the file to the Admin (e.g., ""Successfully scanned X products, belonging to category Y, total quantity Z..."").
                    + Explicitly ask for final confirmation: ""The data is valid and ready. Are you sure you want to proceed with receiving this shipment?"".
                    + WAIT for the Admin's reply.
                6. EXECUTE IMPORT:
                    - If and ONLY IF the Admin issues a confirmation command (e.g., 'Agreed', 'Ok', 'Proceed', 'Let's start', 'Tiến hành', 'Nhập kho đi') AFTER the Review stage:
                        + Call the 'import_approved_inventory_data' function with the corresponding cacheKey, CategoryId, and data.
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

        public async Task<string> AnalyzeHeadersAsync(List<string> headers, string cacheKey, string userMessage)
        {
            var prompt = $@"
            You are an AI assistant analyzing warehouse data.
            The admin has just uploaded an Excel file with the following columns: {string.Join(", ", headers)}.
    
            For successful inventory entry, the system MUST have 4 data fields: Drug Name, Price, Quantity (In Stock), Description and Category.
    
            Task: Write ONE reply message to the Admin:
            1. Acknowledge that the file has been read (Session code is required: `{cacheKey}`).
            2. Analyze whether the existing Excel columns contain ALL 4 required fields (Semantic mapping, e.g., 'Unit Price' = Amount, 'Type' = Category).
            3. IF MISSING: Clearly list the missing fields. Specifically, ask your boss for guidance on how to fill in the missing data (e.g., 'What default price would you like to set?', 'Which category would you like this shipment to be placed in? Do you need me to list the existing categories for you to choose from?').
            4. IF SUFFICIENT: Inform your boss to check the data sheet and type 'Approve' to enter it into inventory.
    
            Return only the message content; absolutely no JSON formatting.";

            var result = await _kernel.InvokePromptAsync(prompt);
            return result.ToString() ?? $"Read the Excel file (Sesion Code: `{cacheKey}`). Please check the data.";
        }
    }
}