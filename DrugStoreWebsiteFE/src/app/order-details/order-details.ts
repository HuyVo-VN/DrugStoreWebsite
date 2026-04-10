import { Component, OnInit } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe, Location } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { OrderService } from '../Services/order.service';
import Swal from 'sweetalert2';
import { LoggerService } from '../Services/logger.service';
import { CartService } from '../Services/cart.service';
import { AppRoles } from '../enums/role.enums';
import { AuthService } from '../Services/auth.service';
import { environment } from '../../environments/environment';


@Component({
  selector: 'app-order-details',
  standalone: true,
  imports: [CommonModule, RouterModule, CurrencyPipe, DatePipe],
  templateUrl: './order-details.html',
  styleUrls: ['./order-details.css']
})
export class OrderDetails implements OnInit {
  public AppRoles = AppRoles;

  userRole = '';
  orderId: string | null = null;
  order: any = null;
  isLoading = false;
  private readonly baseUrl = `${environment.dataApiUrl}`

  subTotal: number = 0;
  shippingFee: number = 3;
  discount: number = 0;

  constructor(
    private authService: AuthService,
    private route: ActivatedRoute,
    private orderService: OrderService,
    private location: Location,
    private logger: LoggerService,
    private cartService: CartService,
    private router: Router
  ) {}

  ngOnInit() {

    this.orderId = this.route.snapshot.paramMap.get('id');
    if (this.orderId) {
      this.loadOrderDetails(this.orderId);
    }

    this.authService.role$.subscribe((role) => {
      this.userRole = role;
    });
  }

  loadOrderDetails(id: string) {
    this.isLoading = true;
    this.orderService.getOrderItemsByOrderId(id).subscribe({
      next: (res) => {
        if (res.status === 200 && res.data) {
          this.order = res.data;
          this.calculateFinancials();
        } else {
          Swal.fire('Error', 'Can not find order', 'error');
          this.goBack();
        }
        this.isLoading = false;
      },
      error: (err) => {
        this.logger.error(err);
        if (err.status === 403) {
            Swal.fire('Forbiden', 'Permision denied', 'error');
        } else {
            Swal.fire('Error', 'Can not load Order Details', 'error');
        }
        this.isLoading = false;
        this.goBack();
      }
    });
  }

  calculateFinancials() {
    if (!this.order || !this.order.items) return;

    this.subTotal = this.order.items.reduce((acc: number, item: any) => {
      return acc + (item.price * item.quantity);
    }, 0);

    const actualPaid = this.order.totalAmount;

    const calculatedDiscount = (this.subTotal + this.shippingFee) - actualPaid;

    this.discount = calculatedDiscount > 0 ? calculatedDiscount : 0;
  }

  reOrder() {
    Swal.fire({
      title: 'Repurchase Order?',
      text: "Items from this order will be added to your shopping cart.",
      icon: 'question',
      showCancelButton: true,
      heightAuto: false,
      confirmButtonColor: '#b1f033ff',
      confirmButtonText: 'Confirm',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        let successCount = 0;
        const items = this.order.items;

        items.forEach((item: any) => {

            this.cartService.addToCart(item.productId, item.quantity).subscribe({
                next: () => successCount++,
                error: () => this.logger.error('Erorr when add ' + item.productName)
            });
        });

        setTimeout(() => {
            Swal.fire('Successfully', 'Item had been added to your cart.', 'success').then(() => {
                this.router.navigate(['/cart']);
            });
        }, 1000);
      }
    });
  }

  getProductImageUrl(imageUrl: string | null): string {
    if (!imageUrl) return '/images/default-product.png';
    if (imageUrl.startsWith('http')) return imageUrl;
    return `${this.baseUrl}${imageUrl}`;
  }

  // Helper css class for status
  getStatusClass(status: string): string {
    switch (status) {
      case 'New': return 'status-new';
      case 'Processing': return 'status-processing';
      case 'Completed': return 'status-completed';
      case 'Cancelled': return 'status-cancelled';
      default: return '';
    }
  }

  public get isCustomer(): boolean {
    return this.userRole === this.AppRoles.Customer;
  }

  goBack() {
    this.location.back();
  }
}
