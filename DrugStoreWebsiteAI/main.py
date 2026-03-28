import os
import time
import asyncio
import json
import re
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from dotenv import load_dotenv

from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_community.utilities import SQLDatabase

# ================== LOAD ENV ==================
load_dotenv()
api_key = os.getenv("GOOGLE_API_KEY")

# ================== FASTAPI ==================
app = FastAPI(title="DrugStore AI Enterprise (With Memory)")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ================== LLM ==================
llm = ChatGoogleGenerativeAI(
    model="gemini-2.5-flash",
    temperature=0,
    max_tokens=2048,
    google_api_key=api_key
)

# ================== BỘ CHỐNG NGHẼN & BỘ NHỚ ==================
last_call_time = 0

# BỘ NHỚ CỦA AI (Lưu lại 5 đoạn hội thoại gần nhất)
chat_history = [] 

def get_formatted_history():
    """Hàm trích xuất lịch sử hội thoại thành chữ để nhét vào não AI"""
    if not chat_history:
        return "Chưa có lịch sử trò chuyện nào trước đó."
    
    history_str = ""
    for chat in chat_history:
        history_str += f"- Giám đốc hỏi: {chat['user']}\n- Thư ký AI đáp: {chat['ai']}\n"
    return history_str

async def safe_llm_call(prompt: str):
    """Gọi LLM an toàn, có rate limit"""
    global last_call_time
    now = time.time()
    if now - last_call_time < 12:
        await asyncio.sleep(12 - (now - last_call_time))
    last_call_time = time.time()
    return await llm.ainvoke(prompt)

# ================== CẤU HÌNH DATABASE ==================
DB_SERVER = r"DESKTOP-ME1OU3E\HUYVO"
DB_USER = "ai_readonly"
DB_PASS = "StrongPassword123!"

db_data_uri = f"mssql+pyodbc://{DB_USER}:{DB_PASS}@{DB_SERVER}/DrugStoreDataDB?driver=ODBC+Driver+17+for+SQL+Server"
db_authen_uri = f"mssql+pyodbc://{DB_USER}:{DB_PASS}@{DB_SERVER}/DrugStoreAuthDB?driver=ODBC+Driver+17+for+SQL+Server"

try:
    db_data = SQLDatabase.from_uri(db_data_uri)
    db_authen = SQLDatabase.from_uri(db_authen_uri)
    print("✅ Cắm ống hút dữ liệu thành công cho cả 2 DB!")
except Exception as e:
    print(f"❌ Lỗi cấu hình DB: {e}")

# ================== TRÍCH XUẤT SQL & ĐỊNH TUYẾN BẰNG AI ==================
async def generate_sql_and_route(query: str, schema_auth: str, schema_data: str, history: str):
    prompt = f"""
    Bạn là chuyên gia cơ sở dữ liệu. Bạn đang quản lý 2 Database độc lập:

    --- DATABASE 1: AUTHEN (Quản lý tài khoản, phân quyền, người dùng, đăng nhập, vai trò) ---
    {schema_auth}

    --- DATABASE 2: DATA (Quản lý sản phẩm, đơn hàng, khách hàng, giỏ hàng, doanh thu, kho) ---
    {schema_data}

    --- LỊCH SỬ TRÒ CHUYỆN GẦN ĐÂY ---
    (Hãy dùng phần này để hiểu ngữ cảnh nếu Giám đốc dùng các từ như "trong đó", "bọn họ", "của chúng", "bao nhiêu cái đã bán"...)
    {history}

    --- CÂU HỎI HIỆN TẠI CỦA GIÁM ĐỐC ---
    "{query}"

    Nhiệm vụ của bạn:
    1. Đọc lịch sử và câu hỏi hiện tại để hiểu Giám đốc đang muốn tìm số liệu gì. Tự suy luận Database (AUTHEN hay DATA).
    2. Viết 1 câu lệnh T-SQL chính xác để lấy số liệu từ Database đó.
    3. Trả về kết quả DƯỚI DẠNG JSON. Không sinh dư văn bản.

    Định dạng JSON BẮT BUỘC:
    {{
        "database": "AUTHEN" hoặc "DATA",
        "sql": "câu lệnh sql của bạn ở đây"
    }}
    """

    response = await safe_llm_call(prompt)
    raw_text = response.content.strip()
    
    match = re.search(r'\{.*\}', raw_text, re.DOTALL)
    if match:
        try:
            parsed_data = json.loads(match.group(0))
            return parsed_data["database"], parsed_data["sql"]
        except Exception as e:
            raise ValueError("AI trả lời sai định dạng JSON.")
    else:
        raise ValueError("Không tìm thấy JSON trong câu trả lời của AI.")

# ================== DỊCH SỐ LIỆU SANG TIẾNG VIỆT ==================
async def format_result(query: str, result: str, history: str):
    prompt = f"""
    Bạn là thư ký AI của nhà thuốc. Dựa vào Lịch sử trò chuyện, Câu hỏi hiện tại và Số liệu thô, hãy trả lời Giám đốc một cách tự nhiên, ngắn gọn bằng tiếng Việt.

    --- Lịch sử trước đó ---
    {history}

    Câu hỏi HIỆN TẠI: "{query}"
    Số liệu DB trả về: "{result}"
    
    Tuyệt đối không nhắc đến SQL hay Database. Trả lời thẳng vào trọng tâm.
    """
    response = await safe_llm_call(prompt)
    return response.content.strip()

# ================== MÔ HÌNH DỮ LIỆU API ==================
class ChatRequest(BaseModel):
    message: str

# ================== API ENDPOINT CHÍNH ==================
@app.post("/api/chatbot/ask")
async def ask_ai(request: ChatRequest):
    global chat_history # Gọi biến bộ nhớ toàn cục ra xài
    
    try:
        user_msg = request.message.strip()
        if not user_msg:
            raise HTTPException(status_code=400, detail="Tin nhắn không được trống")

        print("\n" + "="*50)
        print(f"🚀 Sếp hỏi: {user_msg}")

        # Lấy lịch sử và schema
        current_history = get_formatted_history()
        schema_auth = db_authen.get_table_info() 
        schema_data = db_data.get_table_info() 

        # 1. AI viết SQL (Có nhét thêm trí nhớ vào)
        db_choice, sql_query = await generate_sql_and_route(user_msg, schema_auth, schema_data, current_history)
        
        print(f"📌 Điều hướng: {db_choice}")
        print(f"💻 SQL AI viết: \n{sql_query}")

        # 2. Chạy DB
        db = db_authen if db_choice == "AUTHEN" else db_data
        try:
            db_result = db.run(sql_query)
            print(f"📊 Số liệu thô: {db_result}")
        except Exception as sql_err:
            print(f"❌ Lỗi SQL: {sql_err}")
            return {"status": "error", "reply": "Dạ sếp, câu hỏi này hơi phức tạp, hệ thống tạm thời chưa xử lý được ạ."}

        # 3. Dịch kết quả
        if len(str(db_result)) < 800:
            final_reply = await format_result(user_msg, db_result, current_history)
        else:
            final_reply = f"Dạ, dữ liệu sếp cần đây ạ: {str(db_result)[:800]}..."

        print(f"✅ AI Báo cáo: {final_reply}")
        print("="*50 + "\n")

        # ================= BƯỚC QUAN TRỌNG: GHI NHỚ =================
        # Lưu câu hỏi và câu trả lời vừa rồi vào bộ nhớ
        chat_history.append({"user": user_msg, "ai": final_reply})
        
        # Nếu trí nhớ đầy quá (vượt 5 câu), xóa bớt câu cũ nhất đi để không bị tràn RAM và Token
        if len(chat_history) > 5:
            chat_history.pop(0)

        return {
            "status": "success",
            "sql": sql_query,
            "reply": final_reply
        }

    except Exception as e:
        print(f"❌ Lỗi Backend: {e}")
        raise HTTPException(status_code=500, detail="Hệ thống đang bận, vui lòng thử lại sau.")