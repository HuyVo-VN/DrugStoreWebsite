import { HttpRequest, HttpHandlerFn, HttpEvent, HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError, BehaviorSubject, catchError, filter, take, switchMap } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';


//BehaviorSubject to hold the token refresh state
let isRefreshing = false;
const refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

//connect Access Token to header
const addAuthHeader = (request: HttpRequest<unknown>, token: string): HttpRequest<unknown>=> {
  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}

//Interceptor Function
export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const accessToken = authService.getAccessToken();

  //connect token into request
  let authReq = req;

  if (accessToken && !req.url.includes('/api/Auth/refresh')) {
    authReq = addAuthHeader(req, accessToken);
  }

  return next(authReq).pipe(
    catchError((error: any) => {
      //Only solve 401 (Unauthorized)
      if(error instanceof HttpErrorResponse && error.status == 401){
        //if API refresh also have problem -->> Logout
        if(req.url.includes('/refresh'))
        {
          isRefreshing = false;
          authService.logout();
          return throwError(() => new Error('Refresh token expired'));
        }

        //if is refreshing, let other request wait
        if(authService.getIsRefreshing())
        {
          return refreshTokenSubject.pipe(
            filter(token => token !==  null),
            take(1),
            switchMap((newAccessToken) => {
              return next(addAuthHeader(req, newAccessToken));
            })
          );
        }

        refreshTokenSubject.next(null);

        return authService.refreshToken().pipe(
          switchMap((response: any) => {
            //refresh successfull
            const newAccessToken = response?.data?.accessToken;
            const newRefreshToken = response?.data?.refreshToken;
            if (!newAccessToken) {
               authService.logout();
               return throwError(() => new Error('Invalid refresh response'));
            }

            authService.saveTokens(newAccessToken, newRefreshToken);
            refreshTokenSubject.next(newAccessToken); //notify to orther waiting request

            //send back root request with new token
            return next(addAuthHeader(req, newAccessToken));
          }),

          catchError((refreshError: any) => {
            authService.logout();
            return throwError(() => refreshError);
          })
        );
      }

      return throwError(() => error);
    })
  ) as Observable<HttpEvent<any>>;
};

