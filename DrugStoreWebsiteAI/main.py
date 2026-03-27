import os
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from dotenv import load_dotenv

# --- IMPORT CHUẨN LANGCHAIN MỚI NHẤT ---
from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_community.utilities import SQLDatabase
from langchain_community.agent_toolkits import create_sql_agent

load_dotenv()
api_key = os.getenv("GOOGLE_API_KEY")

app = FastAPI(title="DrugStore AI Service")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- 1. KHỞI TẠO MÔ HÌNH GEMINI ---
llm = ChatGoogleGenerativeAI(
    model="gemini-2.5-flash",
    temperature=0, 
    max_tokens=8192,
    google_api_key=api_key
)

# --- 2. CẤU HÌNH DATABASE SERVER ---
DB_SERVER = r"DESKTOP-ME1OU3E\HUYVO"      
DB_USER = "ai_readonly"                
DB_PASS = "StrongPassword123!"

# --- 3. KHỞI TẠO LANGCHAIN SQL AGENTS ---
agent_data = None
try:
    db_data_uri = f"mssql+pyodbc://{DB_USER}:{DB_PASS}@{DB_SERVER}/DrugStoreDataDB?driver=ODBC+Driver+17+for+SQL+Server"
    db_data = SQLDatabase.from_uri(db_data_uri)
    
    # Cú pháp chuẩn LangChain: Dùng create_sql_agent với agent_type="tool-calling"
    agent_data = create_sql_agent(
        llm=llm, 
        db=db_data, 
        agent_type="tool-calling", 
        verbose=True,
        handle_parsing_errors=True
    )
    print("✅ Kết nối DrugStoreDataDB THÀNH CÔNG!")
except Exception as e:
    print(f"❌ Lỗi kết nối DrugStoreDataDB: {e}")

agent_authen = None
try:
    db_authen_uri = f"mssql+pyodbc://{DB_USER}:{DB_PASS}@{DB_SERVER}/DrugStoreAuthDB?driver=ODBC+Driver+17+for+SQL+Server"
    db_authen = SQLDatabase.from_uri(db_authen_uri)
    
    agent_authen = create_sql_agent(
        llm=llm, 
        db=db_authen, 
        agent_type="tool-calling", 
        verbose=True,
        handle_parsing_errors=True
    )
    print("✅ Kết nối DrugStoreAuthenDB THÀNH CÔNG!")
except Exception as e:
    print(f"❌ Lỗi kết nối DrugStoreAuthenDB: {e}")


# --- 4. HÀM ĐIỀU HƯỚNG CƠ BẢN ---
# Để tránh lỗi của Master Agent cũ, ta dùng thẳng Gemini để điều hướng (Rất an toàn và hiệu quả)
def route_question(query: str) -> str:
    prompt = f"""
    Phân tích câu hỏi và trả về đúng 1 từ (AUTHEN hoặc DATA):
    - AUTHEN: Hỏi về Tài khoản, User, Role, Quyền, Đăng nhập.
    - DATA: Hỏi về Sản phẩm, Đơn hàng, Doanh thu, Thuốc, Khách hàng.
    Câu hỏi: "{query}"
    """
    return llm.invoke(prompt).content.strip().upper()

# --- 6. API ENDPOINT CHÍNH ---
class ChatRequest(BaseModel):
    message: str

@app.post("/api/chatbot/ask")
async def ask_ai(request: ChatRequest):
    print("\n" + "="*50)
    print(f"🚀 ĐÃ NHẬN ĐƯỢC TIN NHẮN TỪ ANGULAR: {request.message}")
    print("="*50 + "\n")
    
    try:
        user_msg = request.message.strip()
        if not user_msg:
            raise HTTPException(status_code=400, detail="Tin nhắn không được trống")
        
        # SỬA DÒNG NÀY: Dùng đúng hàm route_question đã định nghĩa ở trên
        db_choice = route_question(user_msg)
        print(f"🤖 Đã điều hướng vào: {db_choice}")
        
        # Gọi LangChain SQL Agent xử lý
        if "AUTHEN" in db_choice and agent_authen:
            response = agent_authen.invoke({"input": user_msg})
            reply_text = response["output"]
        elif "DATA" in db_choice and agent_data:
            response = agent_data.invoke({"input": user_msg})
            reply_text = response["output"]
        else:
            reply_text = llm.invoke(user_msg).content

        # --- BỘ LỌC ÉP LẤY CHỮ ---
        if isinstance(reply_text, list) and len(reply_text) > 0 and isinstance(reply_text[0], dict):
            # Nếu AI trả về 1 mảng các cục object, moi lấy thuộc tính 'text'
            reply_text = reply_text[0].get("text", str(reply_text))
        elif not isinstance(reply_text, str):
            # Ép kiểu mọi thứ khác về dạng chữ thuần túy
            reply_text = str(reply_text)
            
        return {
            "status": "success",
            "reply": reply_text
        }
    except Exception as e:
        print(f"❌ Lỗi AI Service: {e}")
        raise HTTPException(status_code=500, detail="Hệ thống đang bận, vui lòng thử lại sau.")