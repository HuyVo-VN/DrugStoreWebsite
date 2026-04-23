using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace DrugStoreWebsiteAI.Plugins
{
    public class InventoryPlugin
    {
        // 💡 Sau này sếp tiêm DrugStoreDbContext vào đây để query SQL thật nhé.
        // Hiện tại ta dùng hàm giả lập để test luồng trước.

        [KernelFunction("kiem_tra_ton_kho")]
        [Description("Lấy thông tin số lượng tồn kho của một sản phẩm/thuốc cụ thể. Hữu ích khi người dùng hỏi 'còn bao nhiêu...' hoặc 'kiểm tra kho...'.")]
        public string CheckStock([Description("Tên loại thuốc cần kiểm tra")] string productName)
        {
            // Logic C# truy vấn DB siêu nhanh, siêu tiết kiệm
            var name = productName.ToLower();
            if (name.Contains("panadol")) return "Hiện tại trong kho đang còn 150 hộp Panadol.";
            if (name.Contains("paracetamol")) return "Chỉ còn 5 hộp Paracetamol, sắp hết hàng.";

            return $"Không tìm thấy thông tin tồn kho cho thuốc '{productName}'.";
        }
    }
}