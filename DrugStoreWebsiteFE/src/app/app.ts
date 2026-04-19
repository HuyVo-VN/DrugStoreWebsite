import { Component, signal, OnInit } from '@angular/core';
import { RouterOutlet, RouterModule } from '@angular/router';
import { Login } from './login/login';
import { Home } from './home/home';
import { Register } from './register/register';
import { routes } from './app.routes';
import { ForgetPassword } from './forget-password/forget-password';
import { ResetPassword } from './reset-password/reset-password';
import { AdminPage } from './admin-page/admin-page';
import { User } from './user/user';
import { Footer } from './footer/footer';
import { Header } from './header/header';
import { ChangePassword } from './change-password/change-password';
import { Product } from './product/product';
import { CustomerProduct } from './customer-product/customer-product';
import { Cart } from './cart/cart';
import { CustomerOrder } from './customer-order/customer-order';
import { CommonModule } from '@angular/common';
import { AuthService } from './Services/auth.service';
import { AdminChatbot } from './admin-chatbot/admin-chatbot';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet, Login, Home, Register, ForgetPassword, ResetPassword,
    AdminPage, Footer, Header, User, ChangePassword, Product,
    CustomerProduct, Cart, CustomerOrder, AdminChatbot, CommonModule],

  standalone: true,
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

