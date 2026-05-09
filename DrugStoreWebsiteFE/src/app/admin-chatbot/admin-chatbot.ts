import { Component, OnInit, ViewChild, ElementRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import { MarkdownPipe } from '../markdown-pipe';
import { environment } from '../../environments/environment';

import { AiAgentService } from '../Services/chatbot.service';


interface ChatMessage {
  text: string;
  sender: 'user' | 'ai';
  time: string;
}

@Component({
  selector: 'app-admin-chatbot',
  standalone: true,
  imports: [FormsModule, CommonModule, MarkdownPipe],
  templateUrl: './admin-chatbot.html',
  styleUrl: './admin-chatbot.css'
})
export class AdminChatbot implements OnInit {
  isChatFormOpen: boolean = false;
  currentMessage: string = '';
  selectedFile: File | null = null;

  @ViewChild('chatBody') private chatBodyContent!: ElementRef;

  // 1. MẢNG LƯU LỊCH SỬ CHAT (Khởi tạo sẵn 1 câu chào của AI)
  messages: ChatMessage[] = [
    { text: 'Hi boss! I am the AI ​​assistant.How can I help you today?', sender: 'ai', time: this.getCurrentTime() }
  ];
  isLoading: boolean = false; // Biến hiển thị trạng thái "AI đang gõ..."
  progressText: string = 'Thinking...';

  // 2. NHÚNG HTTP CLIENT ĐỂ GỌI API
  private http = inject(HttpClient);
  private aiService = inject(AiAgentService);
  private apiUrl = `${environment.aiApiUrl}/api/chatbot/ask`; // Đường dẫn tới server Python
  
  constructor() { }

  ngOnInit(): void {
    // 👇 1. KHỞI ĐỘNG KẾT NỐI SIGNALR 👇
    this.aiService.startConnection();

    // 👇 2. LẮNG NGHE ĐÀI PHÁT THANH TỪ BACKEND 👇
    this.aiService.progressMessage$.subscribe(msg => {
      if (msg) {
        this.progressText = msg; // Cập nhật dòng chữ Thinking...
        this.scrollToBottom();
      }
    });
  }

  toggleChatForm(isOpen: boolean) {
    this.isChatFormOpen = isOpen;
    if (isOpen) setTimeout(() => this.scrollToBottom(), 100);
  }

  autoResize(textarea: HTMLTextAreaElement) {
    textarea.style.height = 'auto';
    textarea.style.height = textarea.scrollHeight + 'px';
  }

  // 3. HÀM GỬI TIN NHẮN CHÍNH THỨC
  sendMessage(event: Event, textarea: HTMLTextAreaElement) {
    event.preventDefault();
    const userText = this.currentMessage.trim();

    // Nếu không có cả text lẫn file, hoặc AI đang bận thì không cho gửi
    if ((!userText && !this.selectedFile) || this.isLoading) return;

    // B1: Xử lý hiển thị tin nhắn của User lên UI
    let displayMessage = userText;
    if (this.selectedFile) {
      // Nếu có file, nối thêm tên file vào trước câu chat để Admin dễ nhìn
      displayMessage = `📁 **Attached:** \`${this.selectedFile.name}\`\n` + userText;
    }
    this.messages.push({ text: displayMessage, sender: 'user', time: this.getCurrentTime() });

    // B2: Reset ô nhập liệu
    this.currentMessage = '';
    textarea.style.height = 'auto';

    // B3: Bật trạng thái Loading
    this.isLoading = true;
    this.progressText = 'Thinking...';
    this.scrollToBottom();

    // ==========================================
    // KỊCH BẢN A: ADMIN CÓ GỬI KÈM FILE EXCEL
    // ==========================================
    if (this.selectedFile) {
      // Gọi service xử lý file, truyền cả file lẫn lời dặn của Admin (userText)
      this.aiService.processInventoryExcel(this.selectedFile, userText).subscribe({
        next: (res: any) => {
          this.isLoading = false;
          this.selectedFile = null; // Gửi xong thì xóa file tạm đi

          // Format câu trả lời của AI và chèn Data JSON vào Markdown
          let mdResult = res.reply + `\n\n`;
          if (res.data) {
            mdResult += "```json\n" + JSON.stringify(res.data, null, 2) + "\n```";
          }

          this.messages.push({ text: mdResult, sender: 'ai', time: this.getCurrentTime() });
          this.scrollToBottom();
        },
        error: (err) => {
          this.isLoading = false;
          this.selectedFile = null; // Lỗi cũng phải xóa file tạm

          let realError = "Unknown system error";
          if (err.error?.reply) {
            realError = typeof err.error.reply === 'string' ? err.error.reply : JSON.stringify(err.error.reply);
          } else if (err.error) {
            realError = typeof err.error === 'string' ? err.error : JSON.stringify(err.error);
          } else {
            realError = err.message;
          }

          this.messages.push({
            text: `❌ **File processing error:** \n\`\`\`json\n${realError}\n\`\`\``,
            sender: 'ai',
            time: this.getCurrentTime()
          });
          this.scrollToBottom();
        }
      });
    }
    // ==========================================
    // KỊCH BẢN B: ADMIN CHỈ CHAT TEXT BÌNH THƯỜNG
    // ==========================================
    else {
      // Map toàn bộ mảng messages hiện tại thành định dạng { Role, Content } cho C# đọc
      const historyPayload = this.messages.map(m => ({
        Role: m.sender, // "user" hoặc "ai"
        Content: m.text
      }));

      // Gọi API ask và đẩy nguyên cục lịch sử lên
      this.http.post<any>(this.apiUrl, { messages: historyPayload }).subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.status === 'success') {
            this.messages.push({ text: res.reply, sender: 'ai', time: this.getCurrentTime() });
          }
          this.scrollToBottom();
        },
        error: (err) => {
          this.isLoading = false;

          // Cỗ máy dịch lỗi siêu cấp chống [object Object]
          let realError = "Unknown system error";
          if (err.error?.reply) {
            realError = typeof err.error.reply === 'string' ? err.error.reply : JSON.stringify(err.error.reply);
          } else if (err.error) {
            realError = typeof err.error === 'string' ? err.error : JSON.stringify(err.error);
          } else {
            realError = err.message;
          }

          this.messages.push({
            text: `❌ **Backend reports error:** \n\`\`\`json\n${realError}\n\`\`\`\n\n`,
            sender: 'ai',
            time: this.getCurrentTime()
          });
          this.scrollToBottom();
        }
      });
    }
  }
  onFileSelected(event: any, fileInput: HTMLInputElement): void {
    const file: File = event.target.files[0];
    if (!file) return;

    this.selectedFile = file;
    fileInput.value = '';
  }

  removeSelectedFile() {
    this.selectedFile = null;
  }

  private getCurrentTime(): string {
    const now = new Date();
    return now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  private scrollToBottom(): void {
    try {
      setTimeout(() => {
        this.chatBodyContent.nativeElement.scrollTop = this.chatBodyContent.nativeElement.scrollHeight;
      }, 50);
    } catch (err) { }
  }

}
