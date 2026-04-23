using OfficeOpenXml;

namespace DrugStoreWebsiteAI.Services
{
    public interface IExcelParserService
    {
        // Trả về một List các Dictionary.
        // Ví dụ 1 dòng: {"Mã": "T01", "Tên Thuốc": "Para", "Thành phần": "X", "Giảm giá": "10%"}
        Task<List<Dictionary<string, string>>> ParseExcelDynamicAsync(IFormFile file);
    }

    public class ExcelParserService : IExcelParserService
    {
        public async Task<List<Dictionary<string, string>>> ParseExcelDynamicAsync(IFormFile file)
        {
            var rawDataList = new List<Dictionary<string, string>>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null) throw new Exception("File Excel trống rỗng!");

            var rowCount = worksheet.Dimension.Rows;
            var colCount = worksheet.Dimension.Columns;

            // 1. Quét dòng đầu tiên để lấy danh sách Tên Cột (Headers)
            var headers = new List<string>();
            for (int col = 1; col <= colCount; col++)
            {
                headers.Add(worksheet.Cells[1, col].Text.Trim());
            }

            // 2. Quét các dòng dữ liệu và ghép với Tên Cột tương ứng
            for (int row = 2; row <= rowCount; row++)
            {
                var rowData = new Dictionary<string, string>();
                bool hasData = false;

                for (int col = 1; col <= colCount; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Text.Trim();

                    // Chỉ lấy những ô có dữ liệu để giảm dung lượng JSON gửi cho AI
                    if (!string.IsNullOrEmpty(cellValue))
                    {
                        rowData[headers[col - 1]] = cellValue;
                        hasData = true;
                    }
                }

                if (hasData)
                {
                    rawDataList.Add(rowData);
                }
            }

            return rawDataList;
        }
    }
}