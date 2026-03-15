import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CategoryService {

  // Đường dẫn gốc tới Controller Categories
  private readonly apiUrl = 'https://localhost:5287/api/Categories';

  constructor(private http: HttpClient) { }

  getCategoriesPaged(pageNumber: number, pageSize: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/get-all-paged?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getAllCategories(): Observable<any> {
    return this.http.get(`${this.apiUrl}/get-all`);
  }

  createCategory(data: { name: string; description: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/create`, data);
  }

  updateCategory(id: string, data: { name: string; description: string }): Observable<any> {
    return this.http.put(`${this.apiUrl}/update/${id}`, data);
  }

  updateStatus(categoryId: string, newStatus: boolean): Observable<any> {
    return this.http.patch(`${this.apiUrl}/update-status`, { categoryId, newStatus });
  }

  deleteCategory(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/delete/${id}`);
  }
}
