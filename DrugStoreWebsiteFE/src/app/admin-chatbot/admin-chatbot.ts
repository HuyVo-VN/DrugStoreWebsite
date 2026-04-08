import { Component, OnInit, ViewChild, ElementRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import { MarkdownPipe } from '../markdown-pipe';
import { environment } from '../../environments/environment';


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

  // 2. NHÚNG HTTP CLIENT ĐỂ GỌI API
  private http = inject(HttpClient);
  private apiUrl = `${environment.aiApiUrl}/api/chatbot/ask`; // Đường dẫn tới server Python

  constructor() { }

  ngOnInit(): void { }

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
        this.messages.push({ text: 'Xin lỗi sếp, hệ thống đang bận. Sếp thử lại sau nhé!', sender: 'ai', time: this.getCurrentTime() });
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
