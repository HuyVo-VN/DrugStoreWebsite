import { Component, OnInit, ViewChild, ElementRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import { MarkdownPipe } from '../markdown-pipe';
import { environment } from '../../environments/environment';

import { AiAgentService } from '../Services/ai-agent.service';


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

  @ViewChild('chatBody') private chatBodyContent!: ElementRef;

  // 1. MẢNG LƯU LỊCH SỬ CHAT (Khởi tạo sẵn 1 câu chào của AI)
  messages: ChatMessage[] = [
    { text: 'Chào sếp! Em là trợ lý AI. Sếp cần em giúp gì hôm nay?', sender: 'ai', time: this.getCurrentTime() }
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

    // Nếu tin nhắn rỗng hoặc AI đang trả lời thì không cho gửi
    if (!userText || this.isLoading) return;

    // B1: Đẩy tin nhắn của User vào UI
    this.messages.push({ text: userText, sender: 'user', time: this.getCurrentTime() });

    // B2: Reset ô nhập liệu
    this.currentMessage = '';
    textarea.style.height = 'auto';
    this.scrollToBottom();

    // B3: Bật trạng thái Loading và gọi API
    this.isLoading = true;
    this.progressText = 'Thinking...';
    this.scrollToBottom();

    this.http.post<any>(this.apiUrl, { message: userText }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.status === 'success') {
          // Đẩy câu trả lời của AI vào UI
          this.messages.push({ text: res.reply, sender: 'ai', time: this.getCurrentTime() });
        }
        this.scrollToBottom();
      },
      error: (err) => {
        this.isLoading = false;

        // Cỗ máy dịch lỗi siêu cấp chống [object Object]
        let realError = "Lỗi hệ thống không xác định";
        if (err.error?.reply) {
          realError = typeof err.error.reply === 'string' ? err.error.reply : JSON.stringify(err.error.reply);
        } else if (err.error) {
          realError = typeof err.error === 'string' ? err.error : JSON.stringify(err.error);
        } else {
          realError = err.message;
        }

        this.messages.push({
          text: `❌ **Backend báo lỗi:** \n\`\`\`json\n${realError}\n\`\`\`\n\nSếp copy cục này gửi lại tôi nhé!`,
          sender: 'ai',
          time: this.getCurrentTime()
        });
        this.scrollToBottom();
      }
    });
  }

  onFileSelected(event: any, fileInput: HTMLInputElement): void {
    const file: File = event.target.files[0];
    if (!file) return;

    // In thông báo User đã tải file
    this.messages.push({
      text: `📁 **Đã tải lên tệp:** \`${file.name}\`\nAI bắt đầu bóc tách...`,
      sender: 'user',
      time: this.getCurrentTime()
    });
    this.scrollToBottom();

    this.isLoading = true;
    this.progressText = 'Đang đẩy file lên hệ thống...'; // Chờ SignalR ghi đè

    // Gọi Service bóc tách
    this.aiService.processInventoryExcel(file).subscribe({
      next: (res: any) => {
        this.isLoading = false;
        fileInput.value = ''; // Reset input file

        // Chuyển cục JSON thành Markdown để in ra chatbox cho đẹp
        let mdResult = `✅ **Xử lý hoàn tất!**\nĐã bóc tách được **${res.data?.length || 0}** sản phẩm.\n\n`;
        mdResult += "```json\n" + JSON.stringify(res.data, null, 2) + "\n```";

        this.messages.push({ text: mdResult, sender: 'ai', time: this.getCurrentTime() });
        this.scrollToBottom();
      },
      error: (err) => {
        this.isLoading = false;
        fileInput.value = '';
        this.messages.push({
          text: `❌ **Lỗi bóc tách:** \n\`${err.message}\``,
          sender: 'ai',
          time: this.getCurrentTime()
        });
        this.scrollToBottom();
      }
    });
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
