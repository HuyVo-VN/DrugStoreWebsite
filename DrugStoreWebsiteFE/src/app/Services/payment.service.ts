import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CurrencyService } from './currency.service';


@Injectable({
  providedIn: 'root'
})
export class PaymentService {

  private readonly apiUrl = `${environment.dataApiUrl}/api/Payments`;

  constructor(private http: HttpClient, private currencyService: CurrencyService) { }

  createPaymentUrl(orderId: string, amount: number): Observable<any> {
    const rate = this.currencyService.currentRate;

    const payload = {
      orderId: orderId,
      amount: amount,
      orderDescription: `Pay for the Order ID: ${orderId}`,
      exchangeRate: rate
    };

    return this.http.post(`${this.apiUrl}/create-payment-url`, payload);
  }

  verifyPayment(queryParams: any): Observable<any> {
    return this.http.get(`${this.apiUrl}/payment-callback`, { params: queryParams });
  }
}
