import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Home } from './home/home';
import { ResetPassword } from './reset-password/reset-password';
import { ForgetPassword } from './forget-password/forget-password';
import { Register } from './register/register';
import { AdminPage } from './admin-page/admin-page';
import { Footer } from './footer/footer';
import { Header } from './header/header'; 
import { User } from './user/user';
import { authGuard } from './auth-guard';
import { loginAuthGuard } from './login-auth-guard';
import { ChangePassword } from './change-password/change-password';
import { Product } from './product/product';
import { ProductDetail } from './product-detail/product-detail';
import { CustomerProduct } from './customer-product/customer-product';
import { Cart } from './cart/cart';
import { CustomerOrder } from './customer-order/customer-order';
import { OrderDetails } from './order-details/order-details';
import { OrderManager } from './order-manager/order-manager';
import { AdminBanner } from './admin-banner/admin-banner';
import { AdminCollection } from './admin-collection/admin-collection';
import { PaymentResult } from './payment-result/payment-result';

export const routes: Routes = [
  { path: 'login', component: Login, canActivate: [loginAuthGuard] },
  { path: 'home', component: Home },
  { path: 'reset-password', component: ResetPassword, canActivate: [loginAuthGuard] },
  { path: 'forget-password', component: ForgetPassword, canActivate:[loginAuthGuard] },
  { path: 'register', component: Register },
  { path: 'admin-page', component: AdminPage },
  { path: 'footer', component: Footer },
  { path: 'header', component: Header },
  { path: 'user', component: User, canActivate: [authGuard] },
  { path: 'change-password', component: ChangePassword, canActivate: [authGuard] },
  { path: 'product', component: Product, canActivate: [authGuard] },
  { path: 'product-detail', component: ProductDetail },
  { path: 'cart', component: Cart, canActivate: [authGuard] },
  { path: '', component: CustomerProduct },
  { path: 'customer-orders', component: CustomerOrder, canActivate: [authGuard] },
  { path: 'order-details/:id', component: OrderDetails, canActivate: [authGuard]},
  { path: 'admin/orders', component: OrderManager, canActivate: [authGuard] },
  { path: 'admin/banners', component: AdminBanner, canActivate: [authGuard] },
  { path: 'admin/collections', component: AdminCollection, canActivate: [authGuard] },
  { path: 'payment-result', component: PaymentResult}
];
