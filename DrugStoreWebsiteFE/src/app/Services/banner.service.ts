import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NGXLogger } from 'ngx-logger';

@Injectable({
  providedIn: 'root'
})
export class BannerService {
  readonly apiUrl = 'https://localhost:5287/api/Banners';

  constructor(private http: HttpClient, private logger: NGXLogger) { }

  getActiveBanners(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  getAllBanners(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/all`);
  }

  createBanner(data: FormData): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  updateBanner(id: string, data: FormData): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data);
  }

  deleteBanner(id: string): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }
}
