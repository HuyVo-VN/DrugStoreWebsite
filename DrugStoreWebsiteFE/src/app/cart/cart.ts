import { Component, OnInit } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CartService } from '../Services/cart.service';
import Swal from 'sweetalert2';
import { MatRadioModule } from '@angular/material/radio';
import { LoggerService } from '../Services/logger.service';
import { AuthService } from '../Services/auth.service';
import { UserService } from '../Services/user';
import { OrderService } from '../Services/order.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterModule, CurrencyPipe, FormsModule, MatRadioModule],
  templateUrl: './cart.html',
  styleUrl: './cart.css'
})
export class Cart implements OnInit {
  cartItems: any[] = [];
  grandTotal: number = 0;
  isAllSelected: boolean = true;

  phone: string = '';
  address = '';
  username = '';

  private readonly baseUrl = 'https://localhost:5287';
  private readonly defaultImage = '/images/default-product.png';

  isLoading = false;

  fee: number = 3;
  discount: number = 0.03;
  isDiscount = false;
  discountAmount: number = 0;
  totalToPay: number = 0;
  paymentMethod: string = 'Cash';

  constructor(private cartService: CartService, private logger: LoggerService, private authService: AuthService, private userService: UserService, private orderService: OrderService) { }

  ngOnInit() {
    this.loadCart();
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

  loadCart() {
    this.isLoading = true;
    this.cartService.getCart().subscribe({
      next: (res) => {
        if (res.status === 200 && res.data) {
          this.cartItems = (res.data.items || []).map((item: any) => {
            // auto update when stock is not enough
            if (item.stock > 0 && item.quantity > item.stock) {
              item.quantity = item.stock;
            }

            // check "isActive"
            // if stock=0 or isActive=false
            const isAvailable = item.stock > 0 && item.isActive;

            return {
              ...item,
              selected: isAvailable ? true : false,
              isAvailable: isAvailable
            };
          });

          this.calculateTotal();
          this.cartService.setQuantity(this.cartItems.length);
        } else {
          this.cartItems = [];
        }
        this.isLoading = false;
      },
      error: (err) => this.logger.error('Cart load failed', err)
    });
  }

  calculateTotal() {
    this.grandTotal = this.cartItems
      .filter(item => item.selected && item.isAvailable)
      .reduce((total, item) => {
        const actualPrice = item.discountPercent > 0
          ? item.price - (item.price * item.discountPercent / 100)
          : item.price;

        return total + (actualPrice * item.quantity);
      }, 0);

    if (this.paymentMethod === 'ATM') {
      this.discountAmount = this.grandTotal * 0.03;
    } else {
      this.discountAmount = 0;
    }

    this.totalToPay = this.grandTotal - this.discountAmount + this.fee;

    const availableItems = this.cartItems.filter(i => i.isAvailable);
    this.isAllSelected = availableItems.length > 0 && availableItems.every(i => i.selected);
  }

  toggleAll() {
    this.cartItems.forEach(item => item.selected = this.isAllSelected);
    this.calculateTotal();
  }

  checkItem(item: any) {
    if (!item.isAvailable) {
      item.selected = false;
      return;
    }
    this.calculateTotal();
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

  increaseQty(item: any) {
    item.quantity++;
    this.updateCartItemQuantity(item, item.quantity);
    this.calculateTotal();
  }
  decreaseQty(item: any) {
    item.quantity--;
    if (item.quantity <= 0) {
      this.removeItem(item);
      item.quantity++;
    }
    else {
      this.updateCartItemQuantity(item, item.quantity);
    }
    this.calculateTotal();
  }
  onQtyChange(event: any, item: any) {
    const newValue = parseInt(event.target.value);

    if (isNaN(newValue) || newValue <= 0) {
      this.removeItem(item);
    } else {
      this.updateCartItemQuantity(item, newValue);
    }
  }

  removeItem(item: any) {
    Swal.fire({
      title: 'Are you sure?',
      html: `Do you want to remove?<br>This action cannot be undone.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, delete it!',
      cancelButtonText: 'Cancel',
      heightAuto: false
    }).then((result) => {
      if (result.isConfirmed) {
        this.cartService.removeFromCart(item.itemId)
          .subscribe({
            next: () => {
              Swal.fire({
                icon: 'success',
                title: 'Success',
                text: 'Item has been removed successfully!',
                showConfirmButton: true,
                heightAuto: false,
                customClass: { popup: 'small-swal' }
              });

              this.cartItems = this.cartItems.filter(i => i.productId !== item.productId);
              this.calculateTotal();
              this.cartService.setQuantity(this.cartItems.length);
            },
            error: (err) => {
              Swal.fire({
                icon: 'error',
                title: 'Failed',
                text: err.message,
                showConfirmButton: true,
                heightAuto: false,
                customClass: { popup: 'small-swal' }
              });
            }
          });
      }
    });
  }

  updateCartItemQuantity(item: any, newQty: number) {
    const oldQty = item.quantity;

    item.quantity = newQty;
    this.calculateTotal();

    this.cartService.updateQuantity(item.productId, newQty).subscribe({
      next: (res) => {
        if (res.status === 200 && res.data) {
          this.logger.info('Updated:', res.data);
          item.totalPrice = res.data.TotalPrice;

          this.calculateTotal();
        }
      },
      error: (err) => {
        this.logger.error('Update failed', err);
        item.quantity = oldQty;
        this.calculateTotal();

        Swal.fire('Error', err.error?.message || 'Can not update quantity', 'error');
      }
    });
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
  createOrder() {
    const selectedItems = this.cartItems
      .filter(item => item.selected && item.isAvailable);

    this.orderService.createOrder(this.totalToPay, this.address, this.phone, selectedItems)
      .subscribe({
        next: (response) => {
          if (response.status === 0) {
            Swal.fire({
              icon: 'success',
              title: 'Order created successfully',
              timer: 1500,
              heightAuto: false,
              showConfirmButton: false
            }).then(() => {
              const deleteCalls = selectedItems.map(item =>
                this.cartService.removeFromCart(item.itemId)
              );

              Promise.all(deleteCalls.map(obs => obs.toPromise())).then(() => {
                this.loadCart();
              });

            });
          } else {
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
            text: err.message || 'Something went wrong!',
            timer: 2500,
            showConfirmButton: false,
            heightAuto: false,
          });
        }
      });
  }

}
