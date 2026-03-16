import { Component, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../Services/auth.service';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';
import { UserService } from '../Services/user';
import { AppRoles } from '../enums/role.enums';
import { CartService } from '../Services/cart.service';
import { MatBadgeModule } from '@angular/material/badge';
import { CategoryService } from '../Services/category.service';
import { FormsModule } from '@angular/forms'; 

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterModule, CommonModule, MatBadgeModule, FormsModule],
  templateUrl: './header.html',
  styleUrls: ['./header.css'],
})
export class Header implements OnInit {
  public AppRoles = AppRoles;

  username = '';
  userRole = '';
  email!: string;
  errorMessage = '';
  currentUrl = '';
  showLogin = false;
  quantity: number = 0;

  categories: any[] = [];
  searchKeyword: string = '';
  selectedCategory: string = '';

  constructor(
    private authService: AuthService,
    private router: Router,
    private userService: UserService,
    private cartService: CartService,
    private categoryService: CategoryService
  ) { }

  ngOnInit() {
    this.authService.username$.subscribe((name) => {
      this.username = name;

      if (this.username) {
        this.userService.getUserByUsername(this.username).subscribe({
          next: (res: any) => {
            this.email = res.data.email;
          },
        });
      }
    });

    this.authService.role$.subscribe((role) => {
      this.userRole = role;
      if (this.isCustomer) {
        this.cartService.quantity$.subscribe((quantity) => {
          this.quantity = quantity;
        })
        this.loadCartQuantity();

      }
    });

    this.router.events.subscribe(() => {
      this.currentUrl = this.router.url;
      this.showLoginLink();
    });

    this.categoryService.getAllCategories().subscribe({
      next: (res) => {
        if (res.status === 200 && res.data) {
          this.categories = res.data.filter((cat: any) => cat.isActive === true);
        }
      }
    });

  }


  logout() {
    this.authService.logout();
    Swal.fire({
      icon: 'info',
      title: 'Logged Out',
      text: 'You have been successfully logged out.',
      timer: 1500,
      heightAuto: false,
      showConfirmButton: false,
    }).then(() => {
      this.router.navigate(['/login']);
    });
  }
  login() {
    this.router.navigate(['/login']);
  }
  sendLink() {
    if (!this.email) {
      Swal.fire({
        icon: 'warning',
        title: 'Email Missing',
        text: 'Please wait for the email or login again.',
        heightAuto: false,
        showConfirmButton: true,
      });
      return;
    }
    const token = this.authService.getAccessToken();

    this.authService.forgetPassword(this.email).subscribe({
      next: (res: any) => {
        if (res.status === 404 || !res.data) {
          Swal.fire({
            icon: 'error',
            title: 'User Not Found',
            text: res.message || 'No account found with this email.',
            showConfirmButton: true,
            heightAuto: false,
            customClass: { popup: 'small-swal' },
          });
          return;
        }

        let link = res.data;
        link = link.replace('/reset-password', '/change-password');
        if (token) {
          window.open(link, '_self');
        }
      },
      error: (err) => {
        let errorMessage = 'Invalid email address!';
        if (err.status === 400 && err.error && err.error.errors) {
          const messages = Object.values(err.error.errors).flat();
          if (messages.length > 0) {
            errorMessage = messages.join('\n');
          }
        }
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: errorMessage,
          showConfirmButton: true,
          heightAuto: false,
          customClass: { popup: 'small-swal' },
        });
      },
    });
  }

  showLoginLink() {
    this.showLogin = !this.username && this.currentUrl !== '/login'; // Show login link when not logged in and the current page is not the login page
  }

  onSearch() {
    this.router.navigate(['/'], {
      queryParams: {
        search: this.searchKeyword,
        category: this.selectedCategory
      },
      state: { scrollToGrid: true }
    });
  }

  quickSearch(keyword: string) {
    this.searchKeyword = keyword;
    this.selectedCategory = '';
    this.onSearch();
  }

  onCategoryChange() {
    this.searchKeyword = ''; 
    this.onSearch();
  }

  public get isAdmin(): boolean {
    return this.userRole === this.AppRoles.Admin;
  }

  public get isManager(): boolean {
    return this.userRole === this.AppRoles.Admin || this.userRole === this.AppRoles.Staff;
  }
  public get isCustomer(): boolean {
    return this.userRole === this.AppRoles.Customer;
  }
  public get isGuest(): boolean {
    if (!this.username) return true;
    return false;
  }

  loadCartQuantity() {
    this.cartService.getCart().subscribe({
      next: (res) => {
        const count = res?.data?.items?.length || 0;
        this.cartService.setQuantity(count);
      },
      error: () => this.cartService.setQuantity(0)
    });
  }

  public get showSearch(): boolean {
    const authPages = ['/login', '/register', '/forget-password', '/reset-password', '/change-password'];

    const isAuthPage = authPages.some(page => this.currentUrl.startsWith(page));

    if (isAuthPage) {
      return false;
    }

    return this.isCustomer || this.isGuest;
  }
}
