# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy TOÀN BỘ solution vào ảo (Để AI mượn được code DB của Authen và Data)
COPY . .

# Restore và Build theo đường dẫn mới
RUN dotnet restore "DrugStoreWebsiteAI/DrugStoreWebsiteAI.csproj"
RUN dotnet publish "DrugStoreWebsiteAI/DrugStoreWebsiteAI.csproj" -c Release -o /app/publish

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Đã xóa bỏ đoạn cài đặt ODBC Driver lỗi thời, .NET 8 tự xử lý được kết nối SQL!

COPY --from=build /app/publish .
EXPOSE 8888
ENV ASPNETCORE_URLS=http://+:8888
ENTRYPOINT ["dotnet", "DrugStoreWebsiteAI.dll"]