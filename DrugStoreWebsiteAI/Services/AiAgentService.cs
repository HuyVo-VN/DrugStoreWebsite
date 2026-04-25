using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using System.Text.Json;
using DrugStoreWebsiteAI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DrugStoreWebsiteAI.Services
{
    public interface IAiAgentService
    {
        Task<string> ProcessRawDataWithAiAsync(List<Dictionary<string, string>> rawData);
        Task<string> ProcessRawDataWithAiAsync(List<Dictionary<string, string>> rawData, string connectionId);
        Task<string> ChatAsync(string userMessage);
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
            Bạn là một chuyên gia bóc tách dữ liệu Dược phẩm chuẩn hóa. 
            Tôi sẽ đưa cho bạn một danh sách JSON thô bóc từ Excel. 
            Nhiệm vụ của bạn là phân tích ngữ nghĩa của các 'Key' (Tên cột) và chuyển đổi nó về cấu trúc chuẩn sau:

            1. 'BaseInfo': Chứa các trường cơ bản (Tên, Giá, Số lượng, Danh mục, Trạng thái, Mô tả).
            2. 'SaleInfo': Chứa các trường liên quan đến giảm giá, khuyến mãi, % sale.
            3. 'Specifications': TẤT CẢ các cột còn lại (Thành phần, Chỉ định, Cách dùng, Hạn dùng, Nhà sản xuất...) hãy gom hết vào đây dưới dạng Key-Value.
            4. 'Images': Nếu cột nào chứa link ảnh hoặc tên file ảnh, hãy đưa vào mảng này.

            Yêu cầu: Chỉ trả về duy nhất chuỗi JSON kết quả, không giải thích gì thêm.
            
            DỮ LIỆU ĐẦU VÀO:
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

            await SendProgress(connectionId, "Đang trích xuất Tiêu đề cột gửi cho AI (Siêu tiết kiệm Token)...");

            // 2. CHỈ LẤY TIÊU ĐỀ CỘT (Headers) - Trọng lượng siêu nhẹ!
            var headers = rawData.First().Keys.ToList();
            var headersJson = JsonSerializer.Serialize(headers);

            // 3. PROMPT HOA TIÊU (Chỉ bắt AI tạo Rule Mapping)
            var prompt = $@"
    Mày là chuyên gia phân tích dữ liệu Dược phẩm. 
    Tao có danh sách các tên cột từ file Excel như sau: {headersJson}

    Hãy tạo một bản đồ (Mapping Rule) chuẩn hóa theo định dạng JSON duy nhất.
    Yêu cầu cấu trúc JSON trả về:
    {{
        ""BaseInfo"": [""Mã cột Tên thuốc"", ""Mã cột Giá"", ""Mã cột Số lượng"", ""Mã cột Danh mục""],
        ""SaleInfo"": [""Mã cột Khuyến mãi"", ""Mã cột Giảm giá""],
        ""Images"": [""Mã cột Link ảnh""],
        ""Specifications"": [""Tất cả các cột còn lại (Thành phần, HDSD, NSX...)""]
    }}
    Chỉ trả về JSON, không giải thích. Phải giữ nguyên tên cột gốc có trong danh sách tao đưa.";

            await SendProgress(connectionId, "AI đang lập bản đồ ánh xạ (Mapping Rules)...");

            // Gọi AI (Tốn chưa tới 100 Token)
            var result = await _kernel.InvokePromptAsync(prompt);
            var mappingRuleStr = result.ToString().Replace("```json", "").Replace("```", "").Trim();

            await SendProgress(connectionId, "Đã có bản đồ từ AI! C# đang dùng cơ bắp xử lý hàng ngàn dòng Excel...");

            // 4. DÙNG C# XỬ LÝ TOÀN BỘ DỮ LIỆU TẠI LOCAL (Miễn phí, 1 mili-giây)
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

                    // Quét từng cột trong dòng hiện tại
                    foreach (var kvp in row)
                    {
                        var colName = kvp.Key;
                        var colValue = kvp.Value;

                        if (IsColumnInArray(root, "BaseInfo", colName)) baseInfo[colName] = colValue;
                        else if (IsColumnInArray(root, "SaleInfo", colName)) saleInfo[colName] = colValue;
                        else if (IsColumnInArray(root, "Images", colName)) images.Add(colValue);
                        else specs[colName] = colValue; // Còn lại nhét hết vào Specs
                    }

                    standardizedDataList.Add(new
                    {
                        BaseInfo = baseInfo,
                        SaleInfo = saleInfo,
                        Specifications = specs,
                        Images = images
                    });
                }

                await SendProgress(connectionId, "Xử lý hoàn tất! Trả kết quả về giao diện.");
                return JsonSerializer.Serialize(standardizedDataList);
            }
            catch (Exception ex)
            {
                return $"[{{\"Error\": \"Lỗi khi áp dụng bản đồ AI: {ex.Message}\"}}]";
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

        public async Task<string> ChatAsync(string userMessage)
        {
            try
            {
                var executionSettings = new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };

                var prompt = $@"
                You are the Super Admin AI Agent for a DrugStore ecosystem.
                You have access to 'DrugStore' and 'Auth' databases via SQL Views.

                DECISION MATRIX (Strict Rules):
                1. GREETINGS & CAPABILITIES: If the user says 'hello', 'chào', or asks 'What can you do?' / 'Bạn làm được gì':
                   - DO NOT call any SQL or Export functions.
                   - Just reply warmly in Vietnamese, listing your capabilities (e.g., Tra cứu doanh thu, Kiểm tra kho, Tìm thông tin khách hàng, Xuất báo cáo Excel...) using bullet points.
                
                2. DATABASE QUERIES: If the user explicitly asks for specific data (e.g., 'How many products?', 'Top 5 best sellers', 'Doanh thu hôm nay'):
                   - Call 'get_database_schema' to understand the tables.
                   - Then, call 'execute_readonly_sql_query' to fetch the data.
                   - Format the result beautifully in Markdown.
                
                3. EXPORT FILE: If the user explicitly asks to 'xuất Excel', 'tải file', 'xuất báo cáo':
                   - DO NOT use the readonly query.
                   - Call 'export_data_to_excel' INSTEAD.
                   - Reply with the Markdown hyperlink.

                Admin's input: {userMessage}";

                var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));

                return result.ToString() ?? "Hệ thống AI không trả về kết quả.";
            }
            catch (Microsoft.SemanticKernel.HttpOperationException ex) when (ex.Message.Contains("429"))
            {
                // Bắt gọn lỗi văng 429 của Google
                return "⚠️ **Hệ thống AI đang quá tải.** Hạn mức API Miễn phí chỉ cho phép 15 câu hỏi/phút. Sếp vui lòng đợi khoảng 60 giây rồi thử lại nhé!";
            }
            catch (Exception ex)
            {
                // Các lỗi sập server khác
                return $"❌ **Đã xảy ra lỗi hệ thống:** {ex.Message}";
            }
        }
    }
}