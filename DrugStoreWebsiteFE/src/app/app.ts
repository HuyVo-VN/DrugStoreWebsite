import { Component, signal } from '@angular/core';
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

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Login, Home, Register, ForgetPassword, ResetPassword, AdminPage, Footer, Header, User, ChangePassword, Product, CustomerProduct, Cart, CustomerOrder],
  standalone: true,
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Authentication');
}

