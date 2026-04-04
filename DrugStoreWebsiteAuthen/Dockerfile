# Bước 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ solution (gồm cả Api, Domain, App, Infra...) vào ảo
COPY . .

# Phục hồi và Build project Api (tính từ thư mục gốc đang đứng)
RUN dotnet restore "DrugStoreWebsiteAuthen.Api/DrugStoreWebsiteAuthen.Api.csproj"
RUN dotnet publish "DrugStoreWebsiteAuthen.Api/DrugStoreWebsiteAuthen.Api.csproj" -c Release -o /app/publish

# Bước 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080

# Chạy file dll của project Api
ENTRYPOINT ["dotnet", "DrugStoreWebsiteAuthen.Api.dll"]