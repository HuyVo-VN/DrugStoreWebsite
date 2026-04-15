import { HttpClient } from '@angular/common/http';
import { Injectable, Injector } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { subscriptionLogsToBeFn } from 'rxjs/internal/testing/TestScheduler';
import { Router } from '@angular/router';
import { NGXLogger } from 'ngx-logger';
import { environment } from '../../environments/environment';


@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.authenApiUrl}/api/Auth`;

  private loginStatus = new BehaviorSubject<boolean>(this.isLoggedIn());

  loginStatus$ = this.loginStatus.asObservable();

  private usernameSubject = new BehaviorSubject<string>(this.getUsernameFromToken());
  username$ = this.usernameSubject.asObservable();

  private roleSubject = new BehaviorSubject<string>(this.getRoleFromToken());
  role$ = this.roleSubject.asObservable();

  private isRefreshing = false;

  constructor(private http: HttpClient, private router: Router, private logger: NGXLogger) {
    const token = this.getAccessToken();
    if (token) {
      this.updateUserInfoFromToken(token);
    }
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('access_token');
  }

  getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  public saveTokens(accessToken: string, refreshToken: string): void {
    if (
      !accessToken ||
      !refreshToken ||
      accessToken === 'undefined' ||
      refreshToken === 'undefined'
    ) {
      this.logger.error('Attempted to save invalid tokens. Logging out.');
      this.logout(); // logout if tokens are invalid
      return;
    }

    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', refreshToken);
    //update userinfo when have new token
    this.updateUserInfoFromToken(accessToken);

    this.loginStatus.next(true);
  }

  login(username: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, { username, password }).pipe(
      tap((response: any) => {
        this.saveTokens(response.token, response.refreshToken);
      })
    );
  }

  logout(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    this.loginStatus.next(false);
    this.usernameSubject.next('');
    this.roleSubject.next('');
    this.isRefreshing = false;
    this.router.navigate(['/login']);
  }

  refreshToken(): Observable<any> {
    const refreshToken = this.getRefreshToken();
    const accessToken = this.getAccessToken();
    if (!refreshToken || !accessToken) {
      this.logout();
      return new Observable((observer) => observer.error('Missing tokens for refresh'));
    }

    const body = { AccessToken: accessToken, RefreshToken: refreshToken };
    this.isRefreshing = true;

    return this.http.post(`${this.apiUrl}/refresh`, body);
  }

  public getIsRefreshing(): boolean {
    return this.isRefreshing;
  }

  private decodeToken(token?: string | null): any {
    const t = token || this.getAccessToken();

    if (!t) { return null; }

    try {
      const parts = t.split('.');

      if (parts.length !== 3) {
        return null;
      }

      let payload = parts[1];
      payload = payload.replace(/-/g, '+').replace(/_/g, '/');
      switch (payload.length % 4) {
        case 0:
          break;
        case 2:
          payload += '==';
          break;
        case 3:
          payload += '=';
          break;
        default:
          throw new Error('Invalid Base64Url string!');
      }

      const decodedPayload = atob(payload);
      return JSON.parse(decodedPayload);

    } catch (e) {
      this.logger.error('Error decoding token', e);
      return null;
    }
  }
  private updateUserInfoFromToken(token?: string) {
    const payload = this.decodeToken(token);
    const username = payload?.['unique_name'] || '';
    const role =
      payload?.['role'] ||
      payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
      '';
    const userId = payload?.['sub'] || '';

    this.usernameSubject.next(username);
    this.roleSubject.next(role);
    sessionStorage.setItem('current_user_id', userId);
  }
  getUserId(): string {
    const payload = this.decodeToken();
    return payload?.['sub'] || sessionStorage.getItem('current_user_id') || '';
  }
  getUsernameFromToken(): string {
    const payload = this.decodeToken();
    return payload?.['unique_name'] || '';
  }

  private getRoleFromToken(): string {
    const payload = this.decodeToken();
    return (
      payload?.['role'] ||
      payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
      ''
    );
  }

  getUserRole(): string {
    return this.roleSubject.value || '';
  }

  getUsername(): string {
    return this.usernameSubject.value || 'Anonymous';
  }

  register(userData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, userData);
  }

  forgetPassword(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/forget-password`, { email });
  }

  resetPassword(
    email: string,
    token: string,
    newPassword: string,
    confirmNewPassword: string
  ): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/reset-password`,
      { email, token, newPassword, confirmNewPassword },
      { responseType: 'text' }
    );
  }

  private readonly passwordKey = 'current_password_hash';
  setPassword(password: string) {
    const hash = this.hashPassword(password);
    sessionStorage.setItem(this.passwordKey, hash);
  }

  private hashPassword(password: string): string {
    try {
      return btoa(password);
    } catch {
      return btoa(unescape(encodeURIComponent(password)));
    }
  }

  compareOldPassword(inputPassword: string): boolean {
    const storedHash = sessionStorage.getItem(this.passwordKey);
    if (!storedHash) return false;
    const inputHash = this.hashPassword(inputPassword);
    return storedHash === inputHash;
  }

  clearPassword() {
    sessionStorage.removeItem(this.passwordKey);
  }

  googleLogin(idToken: string) {
    return this.http.post<any>(`${this.apiUrl}/google-login`, { idToken });
  }
}
