import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AiAgentService {
  private hubConnection!: signalR.HubConnection;

  // Biến này đóng vai trò như cái loa phát thanh, Component sẽ "theo dõi" (subscribe) nó
  public progressMessage$ = new BehaviorSubject<string>('');

  private http = inject(HttpClient);

  constructor() { }

  // --- 1. KHỞI ĐỘNG KẾT NỐI SIGNALR (NGHE TIẾN ĐỘ) ---
  public startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.aiApiUrl}/ai-hub`) // Cổng của Backend .NET
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('🟢 Đã kết nối thành công với Tổng đài AI (SignalR)'))
      .catch(err => console.error('🔴 Lỗi kết nối SignalR: ', err));

    // Lắng nghe sự kiện "ReceiveAiProgress" từ C# (AiAgentService.cs) gửi xuống
    this.hubConnection.on('ReceiveAiProgress', (message: string) => {
      this.progressMessage$.next(message); // Đẩy tin nhắn mới ra màn hình
    });
  }

  // --- 2. GỬI FILE EXCEL LÊN BACKEND ---
  public processInventoryExcel(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);

    // Lấy ID kết nối hiện tại để C# biết đường gửi tin nhắn tiến độ về đúng người
    const connectionId = this.hubConnection?.connectionId || '';
    formData.append('connectionId', connectionId);

    // Gọi API của Controller bên C#
    return this.http.post(`${environment.aiApiUrl}/api/ai-agent/process-inventory`, formData);
  }
}
