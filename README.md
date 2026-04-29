# 💊 DrugStoreWebsite - Hệ thống Quản lý & Kinh doanh Nhà thuốc Trực tuyến

**DrugStoreWebsite** là một nền tảng thương mại điện tử chuyên biệt cho ngành dược phẩm, được xây dựng trên kiến trúc hệ thống phân tán (Microservices-lite) với sự hỗ trợ của Trí tuệ nhân tạo (AI) giúp tối ưu hóa việc quản lý tồn kho và tư vấn bán hàng.

## 🏗️ Kiến trúc Hệ thống

Dự án được chia thành 4 phân hệ chính:
- **DrugStoreWebsiteAuthen:** Dịch vụ xác thực, phân quyền (JWT, 2FA, Google Login).
- **DrugStoreWebsiteData:** Dịch vụ xử lý nghiệp vụ lõi (Sản phẩm, Đơn hàng, Giỏ hàng, VNPay).
- **DrugStoreWebsiteAI:** Tác tử AI thông minh (Semantic Kernel, Gemini LLM, Text-to-SQL).
- **DrugStoreWebsiteFE:** Giao diện người dùng và quản trị (Angular).

## 🚀 Hướng dẫn Cài đặt & Triển khai nhanh (Dùng Docker)

Cách nhanh nhất để chạy toàn bộ hệ thống là sử dụng **Docker Compose**.

### 1. Yêu cầu hệ thống
- **Docker Desktop** đã cài đặt.
- **Git** để clone mã nguồn.
- **Google AI API Key** (để chạy phân hệ AI).

### 2. Clone dự án
```bash
git clone [https://github.com/votan/DrugStoreWebsite.git](https://github.com/votan/DrugStoreWebsite.git)
cd DrugStoreWebsite
```

## Cấu hình Biến môi trường (.env) - QUAN TRỌNG

### 📂 File 1: Tại thư mục gốc dự án (./.env)
Dùng để cấu hình tài khoản quản trị cho MinIO (Lưu trữ file).

```bash
MINIO_ADMIN_USER=admin
MINIO_ADMIN_PASS=password123
```
### 📂 File 2: Tại phân hệ Authen (./DrugStoreWebsiteAuthen/DrugStoreWebsiteAuthen.Api/.env)

```bash
CONNECTION_STRING=Server=YOUR_SERVER;Database=DrugStoreAuthDB;User Id=YOUR_USER;Password=YOUR_PASS;
JWT_SECRET=YOUR_SECRET_KEY_32_CHAR
JWT_ISSUER=DrugStore
JWT_AUDIENCE=DrugStoreFE
JWT_EXPIRATION_MINUTES=30
JWT_REFRESH_EXPIRATION_DAYS=15
Google__ClientId=YOUR_GOOGLE_CLIENT_ID
```

### 📂 File 3: Tại phân hệ Data (./DrugStoreWebsiteData/DrugStoreWebSiteData.Api/.env)

```bash
CONNECTION_STRING=Server=YOUR_SERVER;Database=DrugStoreDataDB;User Id=YOUR_USER;Password=YOUR_PASS;
REDIS_CONNECTION=redis-cache:6379
MINIO_ENDPOINT=drugstore-minio:9000
MINIO_ACCESS_KEY=admin
MINIO_SECRET_KEY=password123
VNPAY_TMN_CODE=YOUR_VNPAY_CODE
VNPAY_HASH_SECRET=YOUR_VNPAY_HASH
```
### 📂 File 4: Tại phân hệ AI (./DrugStoreWebsiteAI/.env)

```bash
GEMINI_API_KEY=YOUR_GEMINI_API_KEY_HERE
GEMINI_MODEL=gemini-1.5-flash
# Chuỗi kết nối DB để AI đọc dữ liệu (Dùng IP Docker Gateway nếu chạy container)
DB_CONNECTION_STRING=Server=host.docker.internal;Database=DrugStoreDataDB;User Id=sa;Password=YourPassword;
```
## Khởi chạy bằng Docker
Mở Terminal tại thư mục gốc dự án và chạy:
```bash
docker-compose up -d --build
```
Hệ thống sẽ khả dụng tại:

- Frontend: http://localhost:4200
- MinIO Console: http://localhost:9001  
  (User: admin / Pass: password123)

## 🛠️ Phát triển cục bộ (Local Development)

Nếu bạn muốn chạy từng Project để Debug bằng Visual Studio / VS Code:

### Backend (.NET 8)
- Yêu cầu cài đặt **.NET SDK 8.0**.
- Mở Solution và cập nhật các file `appsettings.json` hoặc tạo file `.env` cục bộ.
- **Lưu ý:** Thay đổi địa chỉ Redis và MinIO từ `redis-cache` thành `localhost` trong cấu hình.
- Chạy lệnh: `dotnet run`.

### Frontend (Angular 17+)
- Cài đặt **Node.js (v18+)**.
- Di chuyển vào thư mục FE: `cd DrugStoreWebsiteFE`.
- Cài đặt thư viện: `npm install`.
- Chạy: `ng serve`

## 🧪 Kiểm thử (Testing)

Dự án đã tích hợp bộ Unit Test sử dụng **xUnit**, **Moq** và **FluentAssertions**.
Để chạy test, di chuyển vào thư mục các project Test và dùng lệnh:

```bash
dotnet test
```

### 5. Phần Bảo mật

## 🛡️ Bảo mật & Tuân thủ

- **2FA:** Tích hợp Google Authenticator cho Admin.
- **Database Isolation:** Tách biệt DB Auth và DB nghiệp vụ.
- **Compliance:** Tuân thủ các quy định về bảo vệ dữ liệu cá nhân theo Nghị định 45/2026/NĐ-CP.

## 👤 Tác giả

- **Võ Tấn Huy** - *Sinh viên thực hiện* - HCMC Open University.
- **Email:** 2254052031huy@ou.edu.vn
- **Người hướng dẫn:** Lê Viết Tuấn.
- **Email:** tuan.lv@ou.edu.vn

*Dự án thuộc khuôn khổ Khóa luận tốt nghiệp ngành Hệ thống thông tin quản lý.*