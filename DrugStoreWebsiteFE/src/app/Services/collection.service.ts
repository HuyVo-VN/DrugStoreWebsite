import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class CollectionService {
  private readonly apiUrl = `${environment.dataApiUrl}/api/Collections`;

  constructor(private http: HttpClient) { }

  getHomepageCollections(limit: number = 5): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/homepage?limit=${limit}`);
  }

  getAllCollectionsAdmin(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/admin/all`);
  }

  createCollection(data: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  updateCollection(id: string, data: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data);
  }

  deleteCollection(id: string): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }
}
