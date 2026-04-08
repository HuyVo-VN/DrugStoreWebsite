import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';


@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly apiUrl = `${environment.dataApiUrl}/api/Cart`;


  private quantitySubject = new BehaviorSubject<number>(0); 
  quantity$ = this.quantitySubject.asObservable();

  constructor(private http: HttpClient) { }

  getCart(): Observable<any> {
    return this.http.get(`${this.apiUrl}/get-cart`);
  }

  setQuantity(quantity: number) {
    this.quantitySubject.next(quantity);
  }
  removeFromCart(itemId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/remove`, { body: { itemId } });
  }

  addToCart(productId: string, quantity: number): Observable<any> {
    const payload = { productId, quantity };
    return this.http.post(`${this.apiUrl}/add`, payload);
  }

  updateQuantity(productId: string, newQuantity: number): Observable<any> {
    const body = {
      productId: productId,
      newQuantity: newQuantity
    };
    return this.http.put(`${this.apiUrl}/update-quantity`, body);
  }

}
