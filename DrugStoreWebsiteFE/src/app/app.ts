import { Component, signal, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './Services/auth.service';
import Swal from 'sweetalert2';

// CHỈ IMPORT NHỮNG THỨ LÀM BỘ KHUNG GIAO DIỆN (LAYOUT)
import { Footer } from './footer/footer';
import { Header } from './header/header';
import { AdminChatbot } from './admin-chatbot/admin-chatbot';

@Component({
  selector: 'app-root',
  standalone: true,

  imports: [
    CommonModule,
    RouterOutlet,
    Header,
    Footer,
    AdminChatbot
  ],

  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('Authentication');

  userRole: string = '';

  constructor(private authService: AuthService) { }

  ngOnInit() {
    // Lắng nghe xem người dùng hiện tại là ai (Khách hay Admin)
    this.authService.role$.subscribe((role) => {
      this.userRole = role;
    });

    Swal.mixin({
      heightAuto: false,
      scrollbarPadding: false
    });
  }
}
