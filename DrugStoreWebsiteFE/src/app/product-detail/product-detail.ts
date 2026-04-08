import { Component, Input, OnInit } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import Swal from 'sweetalert2';
import { ProductService } from '../Services/product.service';
import { CategoryService } from '../Services/category.service';
import { LoggerService } from '../Services/logger.service';
import { AuthService } from '../Services/auth.service';
import { CartService } from '../Services/cart.service';
import { OrderService } from '../Services/order.service';
import { UserService } from '../Services/user';
import { MatRadioModule } from '@angular/material/radio';
import { PaymentService } from '../Services/payment.service';
import { environment } from '../../environments/environment';


@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    CurrencyPipe,
    MatRadioModule
  ],
  templateUrl: './product-detail.html',
  styleUrls: ['./product-detail.css'],
})
export class ProductDetail implements OnInit {

  private readonly baseUrl = `${environment.dataApiUrl}`;
  private readonly defaultImage = '/images/default-product.png';

  loading = false;
  product: any | null = null;
  @Input() productId = '';
  userRole = '';

  quantity = 1;
  min = 1;
  max = 50;
  step = 1;

  originalTotal: number = 0;
  saleDiscountTotal: number = 0;


  totalToPay = 0;
  phone = '';
  address = '';
  hasInput = false;
  item: any[] = [];

  username = '';
  fee: number = 3;
  discount: number = 0.03;
  isDiscount = false;
  discountAmount: number = 0;
  paymentMethod: string = 'Cash';
  grandTotal: number = 0;

  relatedProducts: any[] = [];

  parsedSpecs: any[] = [];
  showSpecPopup: boolean = false;

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    private authService: AuthService,
    private router: Router,
    private logger: LoggerService,
    private cartService: CartService,
    private orderService: OrderService,
    private userService: UserService,
    private paymentService: PaymentService,
    private route: ActivatedRoute,
  ) { }

  ngOnInit() {
    this.productId = this.productService.getProductId();

    if (this.productId) {
      this.getProductById();
    } else {
      this.logger.error("No productId found!");
    }

    this.route.paramMap.subscribe(params => {
      const idFromUrl = params.get('id');

      if (idFromUrl) {
        this.productId = idFromUrl;
        this.getProductById();
      } else {
        this.logger.error("No productId found in URL!");
      }
    });


    this.authService.role$.subscribe((role) => {
      this.userRole = role;
    });

    this.authService.username$.subscribe((name) => {
      this.username = name;

      if (this.username) {
        this.userService.getUserByUsername(this.username).subscribe({
          next: (res: any) => {
            this.phone = res.data.phoneNumber;
          },
        });
        this.getAddress();
      }
    });
  }

  getProductById() {
    this.loading = true;
    this.productService.getProductById(this.productId).subscribe({
      next: (res: any) => {
        this.product = res.data;

        try {
          if (this.product.specifications) {
            this.parsedSpecs = JSON.parse(this.product.specifications);
          } else {
            this.parsedSpecs = [];
          }
        } catch (e) {
          console.error("Error parsing JSON Specifications", e);
          this.parsedSpecs = [];
        }

        this.max = this.product.stock || 0;
        if (this.product.discountPercent > 0 && this.product.discountEndDate) {
          const now = new Date().getTime();
          const endDate = new Date(this.product.discountEndDate).getTime();
          if (endDate > now) {
            const remainingSale = (this.product.saleStock || 0) - (this.product.saleSold || 0);
            if (remainingSale >= 0 && remainingSale < this.max) {
              this.max = remainingSale;
            }
          }
        }
        this.quantity = this.clamp(this.quantity);
        if (this.product.categoryId) {
          this.loadRelatedProducts(this.product.categoryId);
        }
        this.loading = false;
      },
      error: (err) => {
        Swal.fire('Error', 'Failed in loading products', 'error');
        this.loading = false;
      }
    });
  }

  getProductImageUrl(imageUrl: string | null): string {
    if (!imageUrl) return this.defaultImage;
    return imageUrl.startsWith('http') ? imageUrl : `${this.baseUrl}${imageUrl}`;
  }

  addCart() {
    if (this.userRole)
      this.cartService.addToCart(this.productId, this.quantity).subscribe({
        next: () => {
          Swal.fire({
            icon: 'success',
            title: 'Added to Cart',
            text: `Added to your cart!`,
            timer: 1500,
            showConfirmButton: false
          });
          this.cartService.getCart().subscribe(res => {
            const items = res.data?.items || [];
            this.cartService.setQuantity(items.length);
          });
        }
      });
    else {
      Swal.fire({
        icon: 'warning',
        title: 'Login Required',
        text: 'Please log in to continue adding items to your cart!',
        showCancelButton: true,
        confirmButtonText: 'Login',
        cancelButtonText: 'Cancel',
        reverseButtons: true
      }).then((result) => {
        if (result.isConfirmed) {
          window.location.href = '/login';
        }
      });
    }
  }

  inputShippingDetails() {
    this.calculateTotal();
    this.hasInput = true;
  }
  createInstantOrder() {
    if (this.userRole) {
      this.orderService.createInstantOrder(this.totalToPay, this.address, this.phone, this.productId, this.quantity)
        .subscribe({
          next: (response: any) => {
            if (response.status === 0 || response.status === 200) {

              this.hasInput = false;

              if (this.paymentMethod === 'ATM') {
                const createdOrderId = response.data?.id || response.value?.id;

                if (!createdOrderId) {
                  Swal.fire('Error', 'Unable to retrieve the order number to make payment!', 'error');
                  return;
                }

                this.paymentService.createPaymentUrl(createdOrderId, this.totalToPay).subscribe({
                  next: (res) => {
                    if (res.url) {
                      window.location.href = res.url;
                    }
                  },
                  error: () => Swal.fire('Lỗi', 'Unable to create VNPay payment link', 'error')
                });

              }
              else {
                Swal.fire({
                  icon: 'success',
                  title: 'Order created successfully',
                  timer: 1500,
                  heightAuto: false,
                  showConfirmButton: false
                }).then(() => {
                  window.location.reload(); 
                });
              }

            }
            else {
              Swal.fire({
                icon: 'error',
                title: 'Failed',
                text: response.message || 'Something went wrong!',
                timer: 2500,
                showConfirmButton: false,
                heightAuto: false,
              });
            }
          },
          error: (err) => {
            Swal.fire({
              icon: 'error',
              title: 'Failed',
              text: err.error?.message || err.message || 'Something went wrong!',
              timer: 2500,
              showConfirmButton: false,
              heightAuto: false,
            });
          }
        });
    }
    else {
      Swal.fire({
        icon: 'warning',
        title: 'Login Required',
        text: 'Please log in to continue!',
        showCancelButton: true,
        confirmButtonText: 'Login',
        cancelButtonText: 'Cancel',
        reverseButtons: true
      }).then((result) => {
        if (result.isConfirmed) {
          window.location.href = '/login';
        }
      });
    }
  }
  back() {
    this.router.navigate(['/']);
  }
  increase() {
    if (this.quantity >= this.max) {
      this.showMaxLimitWarning();
      return;
    }
    this.quantity = this.clamp(this.quantity + this.step);
  }

  decrease() {
    this.quantity = this.clamp(this.quantity - this.step);
  }

  onInputChange() {
    if (this.quantity > this.max) {
      this.showMaxLimitWarning();
    }
    this.quantity = this.clamp(this.quantity);
  }

  private clamp(v: number) {
    return Math.max(this.min, Math.min(this.max, v));
  }

  getAddress() {
    this.orderService.getAddress().subscribe({
      next: (res: any) => {
        this.address = res.value;
      }
    });
  }

  onPaymentChange(method: string) {
    this.paymentMethod = method;
    this.calculateTotal();
  }

  calculateTotal() {
    this.originalTotal = this.product.price * this.quantity;

    if (this.isSaleActive(this.product)) {
      const discountPerItem = (this.product.price * this.product.discountPercent) / 100;
      this.saleDiscountTotal = discountPerItem * this.quantity;
    } else {
      this.saleDiscountTotal = 0;
    }

    this.grandTotal = this.originalTotal - this.saleDiscountTotal;

    if (this.paymentMethod === 'ATM') {
      this.discountAmount = this.grandTotal * 0.03;
    } else {
      this.discountAmount = 0;
    }

    this.totalToPay = this.grandTotal - this.discountAmount + this.fee;
  }
  closePopup() {
    this.hasInput = false;
  }

  showMaxLimitWarning() {
    if (this.max === this.product.stock) {
      Swal.fire('Sorry', 'Only a limited number of this product left in stock!', 'warning');
    } else {
      Swal.fire('Too late!', 'Only a limited number of Flash Sale slots left!', 'info');
    }
  }

  isSaleActive(item: any): boolean {
    if (!item || !item.discountPercent || item.discountPercent <= 0) return false;
    if (!item.discountEndDate) return false;

    const now = new Date().getTime();
    const endDate = new Date(item.discountEndDate).getTime();

    return endDate > now;
  }

  loadRelatedProducts(categoryId: string) {
    this.productService.filterProducts(categoryId, 1, 6).subscribe({
      next: (res: any) => {
        if (res.isSuccess && res.value && res.value.items) {

          this.relatedProducts = res.value.items.filter((p: any) => {
            return String(p.id).toLowerCase() !== String(this.productId).toLowerCase();
          });

        }
      },
      error: (err) => console.error('Error when loading similar products:', err)
    });
  }

  goToProduct(id: string) {
    this.router.navigate(['/product-detail', id]).then(() => {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    });
  }

}
