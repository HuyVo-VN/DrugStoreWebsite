import { HttpRequest, HttpHandlerFn, HttpEvent, HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError, BehaviorSubject, catchError, filter, take, switchMap } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';


let isRefreshing = false;
const refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

const addAuthHeader = (request: HttpRequest<unknown>, token: string): HttpRequest<unknown> => {
  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
};

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const accessToken = authService.getAccessToken();

  let authReq = req;

  if (accessToken && !req.url.includes('/login') && !req.url.includes('/register') && !req.url.includes('/refresh')) {
    authReq = addAuthHeader(req, accessToken);
  }

  return next(authReq).pipe(
    catchError((error: any) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {

        if (req.url.includes('/refresh')) {
          isRefreshing = false;
          authService.logout();
          return throwError(() => new Error('Refresh token expired'));
        }

        if (isRefreshing) {
          return refreshTokenSubject.pipe(
            filter(token => token !== null),
            take(1),
            switchMap((newAccessToken) => {
              return next(addAuthHeader(req, newAccessToken));
            })
          );
        }

        isRefreshing = true;
        refreshTokenSubject.next(null);

        return authService.refreshToken().pipe(
          switchMap((response: any) => {
            isRefreshing = false; 

            const newAccessToken = response?.data?.accessToken || response?.data?.AccessToken;
            const newRefreshToken = response?.data?.refreshToken || response?.data?.RefreshToken;

            if (!newAccessToken) {
              authService.logout();
              return throwError(() => new Error('Invalid refresh response'));
            }

            authService.saveTokens(newAccessToken, newRefreshToken);

            refreshTokenSubject.next(newAccessToken);

            return next(addAuthHeader(req, newAccessToken));
          }),

          catchError((refreshError: any) => {
            isRefreshing = false;
            authService.logout();
            return throwError(() => refreshError);
          })
        );
      }

      return throwError(() => error);
    })
  ) as Observable<HttpEvent<any>>;
};
