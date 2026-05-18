import { Component, OnInit } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { OrderService } from '../Services/order.service';
import { AuthService } from '../Services/auth.service';
import Swal from 'sweetalert2';
import { LoggerService } from '../Services/logger.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { OrderStatus } from '../enums/status.enums';
import { environment } from '../../environments/environment';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CurrencyConverterPipe } from '../currency-converter.pipe';

@Component({
  selector: 'app-customer-order',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, CurrencyConverterPipe, DatePipe, MatFormFieldModule, MatSelectModule, MatInputModule, FormsModule],
  templateUrl: './customer-order.html',
  styleUrls: ['./customer-order.css']
})
export class CustomerOrder implements OnInit {

  private readonly baseUrl = `${environment.dataApiUrl}`;
  private readonly defaultImage = '/images/default-product.png';

  orders: any[] = [];
  isLoading = false;

  showFilter = false;

  minPrice: number | null = null;
  maxPrice: number | null = null;

  value = '';
  viewValue = '';
  selectedStatus = '';
  filter = '';

  // Sử dụng Getter để mảng này luôn tự cập nhật ngôn ngữ khi user chuyển đổi
  get statusList() {
    return [
      { value: OrderStatus.New, viewValue: this.getStatus(0) },
      { value: OrderStatus.Processing, viewValue: this.getStatus(1) },
      { value: OrderStatus.Completed, viewValue: this.getStatus(2) },
      { value: OrderStatus.Cancelled, viewValue: this.getStatus(3) }
    ];
  }

  //pagination
  currentPage = 1;
  pageSize = 4;
  totalPages = 0;
  totalCount = 0;
  pages: number[] = [];

  constructor(
    private orderService: OrderService,
    private authService: AuthService,
    private router: Router,
    private logger: LoggerService,
    private translate: TranslateService
  ) { }

  ngOnInit() {
    this.loadOrders();
  }

  loadOrders() {
    this.isLoading = true;
    this.orderService.getCustomerOrders(this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        if (res.status === 200 && res.data) {
          const pagedResult = res.data;

          this.orders = pagedResult.items;

          //pagiation
          this.totalPages = pagedResult.totalPages || 0;
          this.totalCount = pagedResult.totalCount || 0;
          this.pages = Array.from({ length: this.totalPages }, (_, i) => i + 1);

          // Scroll to top when switch page
          window.scrollTo({ top: 0, behavior: 'smooth' });
        } else {
          this.orders = [];
        }
        this.isLoading = false;
      },
      error: (err) => {
        this.logger.error('Failed to load orders', err);
        Swal.fire(this.translate.instant('COMMON.ERROR'), 'Failed to load orders', 'error');
        this.isLoading = false;
      }
    });
  }

  onPageChange(page: number) {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }
    this.currentPage = page;
    if (this.selectedStatus) {
      this.toggleFilter();
      this.applyFilter();
      return;
    }
    this.loadOrders();
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'New': return 'status-new';
      case 'Processing': return 'status-processing';
      case 'Completed': return 'status-completed';
      case 'Cancelled': return 'status-cancelled';
      default: return '';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'New':
        return this.translate.instant('ORDER.BADGE_NEW') !== 'ORDER.BADGE_NEW' ? this.translate.instant('ORDER.BADGE_NEW') : 'New Proceed';
      case 'Processing':
        return this.translate.instant('ORDER.BADGE_PROCESSING') !== 'ORDER.BADGE_PROCESSING' ? this.translate.instant('ORDER.BADGE_PROCESSING') : 'Processing';
      case 'Completed':
        return this.translate.instant('ORDER.BADGE_COMPLETED') !== 'ORDER.BADGE_COMPLETED' ? this.translate.instant('ORDER.BADGE_COMPLETED') : 'Completed';
      case 'Cancelled':
        return this.translate.instant('ORDER.BADGE_CANCELLED') !== 'ORDER.BADGE_CANCELLED' ? this.translate.instant('ORDER.BADGE_CANCELLED') : 'Cancelled';
      default: return status;
    }
  }

  getStatus(status: number): string {
    switch (status) {
      case 0:
        return this.translate.instant('ORDER.BADGE_NEW') !== 'ORDER.BADGE_NEW' ? this.translate.instant('ORDER.BADGE_NEW') : 'New Proceed';
      case 1:
        return this.translate.instant('ORDER.BADGE_PROCESSING') !== 'ORDER.BADGE_PROCESSING' ? this.translate.instant('ORDER.BADGE_PROCESSING') : 'Processing';
      case 2:
        return this.translate.instant('ORDER.BADGE_COMPLETED') !== 'ORDER.BADGE_COMPLETED' ? this.translate.instant('ORDER.BADGE_COMPLETED') : 'Completed';
      case 3:
        return this.translate.instant('ORDER.BADGE_CANCELLED') !== 'ORDER.BADGE_CANCELLED' ? this.translate.instant('ORDER.BADGE_CANCELLED') : 'Cancelled';
      default: return '';
    }
  }

  viewDetails(orderId: string) {
    this.router.navigate(['/order-details', orderId]);
  }

  getProductImageUrl(imageUrl: string | null): string {
    if (!imageUrl || imageUrl === 'null') {
      return this.defaultImage;
    }
    if (imageUrl.startsWith('http')) {
      return imageUrl;
    }

    const cleanPath = imageUrl.startsWith('/') ? imageUrl : `/${imageUrl}`;
    return `${this.baseUrl}${cleanPath}`;
  }

  toggleFilter() {
    this.showFilter = !this.showFilter;
  }

  applyFilter() {
    this.toggleFilter();
    if (!this.selectedStatus) {
      return;
    }
    this.orderService.filterOrders(Number(this.selectedStatus), this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        if (res.value) {
          const pagedResult = res.value;
          this.orders = pagedResult.items;
          this.totalPages = pagedResult.totalPages;
          this.totalCount = pagedResult.totalCount;
          this.filterStatusSelected();
        }
      },
      error: (err) => {
        Swal.fire({
          icon: 'error',
          title: this.translate.instant('COMMON.ERROR') || 'Failed',
          text: err.error || 'Failed to filter orders.',
          heightAuto: false,
        });
      }
    });
  }

  reset() {
    this.minPrice = null;
    this.maxPrice = null;
    this.selectedStatus = '';
    this.filter = '';
    this.toggleFilter();
    this.loadOrders();
  }

  filterStatusSelected() {
    this.filter = this.getStatus(Number(this.selectedStatus)) || '';
  }

  resetFilterStatus() {
    this.filter = '';
    this.selectedStatus = '';
    this.loadOrders();
  }

}
