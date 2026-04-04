FROM python:3.11-slim

# Cài đặt Microsoft ODBC Driver 17 theo chuẩn Debian mới (Không dùng apt-key)
RUN apt-get update && apt-get install -y curl apt-transport-https gnupg2 unixodbc-dev \
    && curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg \
    && curl -fsSL https://packages.microsoft.com/config/debian/12/prod.list > /etc/apt/sources.list.d/mssql-release.list \
    && apt-get update \
    && ACCEPT_EULA=Y apt-get install -y msodbcsql17 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Cài thư viện Python
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy code vào
COPY . .

EXPOSE 8888
# Lệnh chạy FastAPI
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8888"]