import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private readonly apiUrl = 'https://localhost:5287/api/Order';

  constructor(private http: HttpClient) { }

  getCustomerOrders(page: number, pageSize: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/customer-orders?pageNumber=${page}&pageSize=${pageSize}`);
  }

  getOrderItemsByOrderId(id: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/${id}`);
  }

  getAllOrders(): Observable<any> {
    return this.http.get(`${this.apiUrl}/all`);
  }

  updateOrderStatus(orderId: string, newStatus: number): Observable<any> {
    const body = {
      orderId: orderId,
      newStatus: newStatus
    };
    return this.http.patch(`${this.apiUrl}/update-status-order`, body);
  }

  deleteOrder(orderId: string): Observable<any> {
    const requestBody = {
      OrderId: orderId
    };

    return this.http.delete(`${this.apiUrl}/delete-order`, { body: requestBody, responseType: 'text' });
  }

  createOrder(totalAmount: number, shippingAddress: string, phoneNumber: string, items: any[]): Observable<any> {
    const payload = {
      totalAmount: totalAmount,
      shippingAddress: shippingAddress,
      phoneNumber: phoneNumber,
      items: items.map(item => ({
        productId: item.productId,
        quantity: item.quantity
      }))
    };

    return this.http.post(`${this.apiUrl}/create-order`, payload);
  }

  filterOrders(Status: number, page: number, pageSize: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/filter-customer-orders?status=${Status}&pageNumber=${page}&pageSize=${pageSize}`);
  }

  createInstantOrder(totalAmount: number, shippingAddress: string, phoneNumber: string, productId: string, quantity: number): Observable<any> {
    const payload = {
      totalAmount: totalAmount,
      shippingAddress: shippingAddress,
      phoneNumber: phoneNumber,
      items: [
        {
          productId: productId,
          quantity: quantity
        }
      ]
    }
    return this.http.post(`${this.apiUrl}/create-order`, payload);
  }
  getAddress():Observable<any>{
    return this.http.get(`${this.apiUrl}/get-customer-address`);
  }
}