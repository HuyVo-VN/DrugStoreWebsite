import { Component, OnInit } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { OrderService } from '../Services/order.service';
import Swal from 'sweetalert2';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-order-manager',
  standalone: true,
  imports: [CommonModule, RouterModule, CurrencyPipe, DatePipe, FormsModule],
  templateUrl: './order-manager.html',
  styleUrls: ['./order-manager.css']
})
export class OrderManager implements OnInit {
  
  orders: any[] = [];
  isLoading = false;
  
  statusOptions = [
    { value: 0, label: 'New' },
    { value: 1, label: 'Paid' },
    { value: 2, label: 'Processing' },
    { value: 3, label: 'Completed' },
    { value: 4, label: 'Cancelled' },
  ];

  constructor(
    private orderService: OrderService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadAllOrders();
  }

  loadAllOrders() {
    this.isLoading = true;
    this.orderService.getAllOrders().subscribe({
      next: (res) => {
        if (res.status === 200 && res.data) {
          this.orders = res.data;
        } else {
          this.orders = [];
        }
        this.isLoading = false;
      },
      error: (err) => {
        Swal.fire('Error', 'Failed to load orders', 'error');
        this.isLoading = false;
      }
    });
  }

  openStatusModal(order: any) {
    const optionsHtml = this.statusOptions.map(opt => 
        `<option value="${opt.value}" ${order.status === opt.label ? 'selected' : ''}>${opt.label}</option>`
    ).join('');

    Swal.fire({
      title: 'Update Order Status',
      html: `
        <div style="text-align: left; margin-top: 10px; margin-left: 10%;">
           <label style="font-weight:bold;">Order ID:</label> <br> #${order.id} <br><br>
           <label style="font-weight:bold;">Current Status:</label> <br> 
           <span class="badge status-${order.status.toLowerCase()}">${order.status}</span> <br><br>
           
           <label style="font-weight:bold;">New Status:</label>
           <select id="swal-status-select" class="swal2-select" style="width:90%; display:flex; margin-left: 0%;">
             ${optionsHtml}
           </select>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Update',
      confirmButtonColor: '#3792b3',
      preConfirm: () => {
        return parseInt((document.getElementById('swal-status-select') as HTMLSelectElement).value);
     }
    }).then((result) => {
      if (result.isConfirmed) {
        const newStatus = parseInt(result.value);
        this.updateStatus(order.id, newStatus);
      }
    });
  }

  updateStatus(orderId: string, statusValue: number) {
    this.orderService.updateOrderStatus(orderId, statusValue).subscribe({
      next: (res) => {
        Swal.fire('Updated!', 'Order status has been updated.', 'success');
        this.loadAllOrders(); 
      },
      error: (err) => {
        const errorMsg = err.error || 'Update failed'; 
        Swal.fire('Failed', errorMsg, 'error');
      }
    });
  }

  deleteOrder(order: any) {
    Swal.fire({
      title: 'Are you sure?',
      text: `Do you want to delete Order #${order.id.substring(0, 8)}? This action cannot be undone.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
      if (result.isConfirmed) {
        
        this.orderService.deleteOrder(order.id).subscribe({
          next: (res) => {
            Swal.fire(
              'Deleted!',
              'The order has been deleted.',
              'success'
            );
            this.loadAllOrders();
          },
          error: (err) => {
            const errorMsg = err.error || 'Failed to delete order';
            Swal.fire('Error', errorMsg, 'error');
          }
        });
      }
    });
  }

  viewDetails(orderId: string) {
    this.router.navigate(['/order-details', orderId]);
  }

  getStatusClass(status: string): string {
    return 'status-' + status.toLowerCase();
  }
}
