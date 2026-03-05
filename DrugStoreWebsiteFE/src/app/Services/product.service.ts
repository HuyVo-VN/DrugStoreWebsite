import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { NGXLogger } from 'ngx-logger';


@Injectable({
  providedIn: 'root'
})
export class ProductService {

  private readonly apiUrl = 'https://localhost:5287/api/Products';

  constructor(private http: HttpClient, private logger: NGXLogger) { }


  //call API create product
  createProduct(productData: FormData): Observable<any> {
    // Interceptor will handle the token
    return this.http.post(`${this.apiUrl}/create`, productData);
  }
  //call API get products
  getProducts(page: number, pageSize: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/get-all?pageNumber=${page}&pageSize=${pageSize}`);
  }

  //call API delete product
  deleteProduct(ProductId: string): Observable<any> {
    const token = localStorage.getItem('access_token');
    let UpdatedBy = 'Unknown';

    if (token) {
      try {
        const payloadPart = token.split('.')[1];
        const decoded = JSON.parse(atob(payloadPart));
        UpdatedBy = decoded?.['unique_name'] || decoded?.['sub'] || 'Unknown';
      } catch (e) {
        this.logger.error('Failed to decode token', e);
      }
    }
    const UpdatedAt = new Date().toISOString();

    const payload = {
      ProductId,
      UpdatedBy,
      UpdatedAt
    };
    return this.http.delete(`${this.apiUrl}/delete-product`, { body: payload });
  }

  updateStatusProduct(ProductId: string, NewStatus: boolean): Observable<any> {
    const token = localStorage.getItem('access_token');
    let UpdatedBy = 'Unknown';

    if (token) {
      try {
        const payloadPart = token.split('.')[1];
        const decoded = JSON.parse(atob(payloadPart));
        UpdatedBy = decoded?.['unique_name'] || decoded?.['sub'] || 'Unknown';
      } catch (e) {
        this.logger.error('Failed to decode token', e);
      }
    }
    const UpdatedAt = new Date().toISOString();

    const payload = {
      ProductId,
      NewStatus,
      UpdatedBy,
      UpdatedAt
    };
    return this.http.patch(`${this.apiUrl}/update-status-product`, payload);
  }

  //call API update product
  updateProduct(id: string, productData: FormData): Observable<any> {
    return this.http.put(`${this.apiUrl}/update/${id}`, productData);
  }

  searchProducts(ProductName: string, PageNumber: number, PageSize: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/search-product?ProductName=${ProductName}&PageNumber=${PageNumber}&PageSize=${PageSize}`);
  }

  filterProducts(CategoryId: string, PageNumber: number, PageSize: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/filter-products?categoryId=${CategoryId}&pageNumber=${PageNumber}&pageSize=${PageSize}`);
  }

  getProductById(id: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/${id}`);
  }
  
  private _productId: string = '';

  setProductId(id: string) {
    this._productId = id;
  }

  getProductId(): string {
    return this._productId;
  }

  getSaleProducts(limit: number = 10): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/sale-products?limit=${limit}`);
  }

  getBestSellers(limit: number = 10): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/best-sellers?limit=${limit}`);
  }

  getProductsByCollectionName(collectionName: string, take: number = 5): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/collection/${encodeURIComponent(collectionName)}?take=${take}`);
  }
}
