import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CategoryService {

  private readonly apiUrl = 'https://localhost:5287/api/Categories'; 

  constructor(private http: HttpClient) { }


  getCategories(): Observable<any> {
    return this.http.get(`${this.apiUrl}`); 
  }
}