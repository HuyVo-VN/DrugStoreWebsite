import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// Cấu trúc dữ liệu API C# trả về
export interface ExportResponse {
  downloadUrl: string;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  // Trỏ đúng vào cổng 5002 của Data API
  private apiUrl = `${environment.dataApiUrl}/api/Dashboard`;

  constructor(private http: HttpClient) { }

  exportDynamicExcel(entity: string, statType: string, year?: any, month?: any): Observable<any> {
    let params = new HttpParams().set('entity', entity).set('statType', statType);
    if (year && year !== 'all') params = params.set('year', year.toString());
    if (month && month !== 'all') params = params.set('month', month.toString());
    return this.http.get<ExportResponse>(`${this.apiUrl}/export-excel`, { params });
  }

  exportDynamicPdf(entity: string, statType: string, year?: any, month?: any): Observable<any> {
    let params = new HttpParams().set('entity', entity).set('statType', statType);
    if (year && year !== 'all') params = params.set('year', year.toString());
    if (month && month !== 'all') params = params.set('month', month.toString());
    return this.http.get<ExportResponse>(`${this.apiUrl}/export-pdf`, { params });
  }

  getChartData(entity: string, statType: string, year?: any, month?: any): Observable<any> {
    let params = new HttpParams().set('entity', entity).set('statType', statType);
    if (year && year !== 'all') params = params.set('year', year.toString());
    if (month && month !== 'all') params = params.set('month', month.toString());
    return this.http.get(`${this.apiUrl}/chart-data`, { params });
  }
}
