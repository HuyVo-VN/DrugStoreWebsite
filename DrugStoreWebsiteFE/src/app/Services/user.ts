import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { NGXLogger } from 'ngx-logger';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly apiUrl = `${environment.authenApiUrl}/api/User`;
  constructor(private http: HttpClient, private logger: NGXLogger) { }

  getUsers(): Observable<any> {
    return this.http.get(`${this.apiUrl}/get-users`);
  }
  assignRole(userId: string, role: string) {
    return this.http.post(`${this.apiUrl}/assign-role`, { userId, roleName: role });
  }
  getUserByUsername(userName: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/get-user-by-username`, { params: { userName } });
  }
  deleteUser(userId: string): Observable<any> {
    const token = localStorage.getItem('access_token');
    let updatedBy = 'Unknown';

    if (token) {
      try {
        const payloadPart = token.split('.')[1];
        const decoded = JSON.parse(atob(payloadPart));
        updatedBy = decoded?.['unique_name'] || decoded?.['sub'] || 'Unknown';
      } catch (e) {
        this.logger.error('Failed to decode token', e);
      }
    }

    const updatedAt = new Date().toISOString();

    const payload = {
      userId,
      updatedBy,
      updatedAt
    };

    return this.http.delete(`${this.apiUrl}/delete`, { body: payload });

  }

}

