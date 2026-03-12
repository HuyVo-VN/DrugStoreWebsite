import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common'; 
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentService } from '../Services/payment.service';

@Component({
  selector: 'app-payment-result',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payment-result.html',
  styleUrl: './payment-result.css'
})
export class PaymentResult implements OnInit {
  isSuccess = false;
  isLoading = true;

  constructor(
    private route: ActivatedRoute,
    private paymentService: PaymentService,
    private router: Router
  ) { }

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['vnp_SecureHash']) {
        this.paymentService.verifyPayment(params).subscribe({
          next: (res) => {
            this.isSuccess = true;
            this.isLoading = false;
          },
          error: (err) => {
            console.error("Lỗi xác thực VNPay: ", err);
            this.isSuccess = false;
            this.isLoading = false;
          }
        });
      } else {
        this.isLoading = false;
        this.isSuccess = false;
      }
    });
  }

  goToHome() {
    this.router.navigate(['/']);
  }
}
