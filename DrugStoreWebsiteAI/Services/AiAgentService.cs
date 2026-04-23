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
            // 1. Báo cáo: Bắt đầu
            await SendProgress(connectionId, "Đang đóng gói dữ liệu gửi lên Google Gemini...");
            var jsonInput = JsonSerializer.Serialize(rawData);

            var prompt = $@"
            Mày là một chuyên gia bóc tách dữ liệu Dược phẩm chuẩn hóa. 
            Tao sẽ đưa cho mày một danh sách JSON thô bóc từ Excel. 
            Nhiệm vụ của mày là phân tích ngữ nghĩa của các 'Key' (Tên cột) và chuyển đổi nó về cấu trúc chuẩn sau:
            1. 'BaseInfo': Chứa các trường cơ bản (Mã, Tên, Giá, Số lượng, Danh mục, Trạng thái).
            2. 'SaleInfo': Chứa các trường liên quan đến giảm giá, khuyến mãi, % sale.
            3. 'Specifications': TẤT CẢ các cột còn lại (Thành phần, Chỉ định, Cách dùng, Hạn dùng...) hãy gom hết vào đây dưới dạng Key-Value.
            4. 'Images': Nếu cột nào chứa link ảnh hoặc tên file ảnh, hãy đưa vào mảng này.
            Yêu cầu: Chỉ trả về duy nhất chuỗi JSON kết quả, không giải thích gì thêm.
            DỮ LIỆU ĐẦU VÀO:
            {jsonInput}";

            // 2. Báo cáo: Đang suy luận
            await SendProgress(connectionId, "AI đang suy luận và phân loại dữ liệu (có thể mất vài giây)...");
            var result = await _kernel.InvokePromptAsync(prompt);
            
            // 3. Báo cáo: Dọn dẹp
            await SendProgress(connectionId, "Đã nhận kết quả! Đang dọn dẹp định dạng JSON...");
            var responseText = result.ToString() ?? "[]";
            responseText = responseText.Replace("```json", "").Replace("```", "").Trim();

            // 4. Báo cáo: Hoàn thành
            await SendProgress(connectionId, "Xử lý hoàn tất! Đang trả kết quả về màn hình...");
            return responseText;
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
            var executionSettings = new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var prompt = $@"
            You are the Super Admin AI Agent for a DrugStore ecosystem.
            You have access to 'DrugStore' and 'Auth' databases via SQL Views.

            DECISION MATRIX:
            1. If the user asks a conversational question or wants a quick summary (e.g., 'How many products?', 'Top 5 best sellers'):
               - Call 'execute_readonly_sql_query'.
               - Format the result beautifully in Markdown.
            
            2. If the user explicitly asks to 'xuất Excel', 'tải file', 'báo cáo toàn bộ' (e.g., 'Xuất danh sách sản phẩm ra Excel'):
               - DO NOT use the readonly query.
               - Call 'export_data_to_excel' INSTEAD.
               - Reply to the user in Vietnamese with a Markdown hyperlink to download the file. Example: '[Nhấn vào đây để tải báo cáo](url)'

            Admin's input: {userMessage}";

            var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));

            return result.ToString() ?? "System timeout. Please try again.";
        }
    }
}