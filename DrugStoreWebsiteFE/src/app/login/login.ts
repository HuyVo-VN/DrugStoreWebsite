import { Component, ViewChild, ElementRef, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../Services/auth.service';
import Swal from 'sweetalert2';
import { LoggerService } from '../Services/logger.service';
import { SocialAuthService, GoogleLoginProvider, SocialAuthServiceConfig } from '@abacritt/angularx-social-login';
import { GoogleSigninButtonModule } from '@abacritt/angularx-social-login';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterModule, GoogleSigninButtonModule, CommonModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
})
export class Login implements OnInit {
  username = '';
  password = '';
  message = '';

  // Biến điều khiển 2FA
  requires2FA = false;
  otpCode = '';

  isGoogleProcessing = false;

  @ViewChild('passwordInput') passwordInput!: ElementRef;

  constructor(
    private http: HttpClient,
    private router: Router,
    private authService: AuthService,
    private logger: LoggerService,
    private socialAuthService: SocialAuthService
  ) { }

  ngOnInit() {
    this.socialAuthService.authState.subscribe((googleUser) => {
      if (!googleUser || !googleUser.idToken || this.isGoogleProcessing) {
        return;
      }

      this.isGoogleProcessing = true;

      this.authService.googleLogin(googleUser.idToken).subscribe({
        next: (res: any) => {

          this.isGoogleProcessing = false;

          if (res.requires2FA) {
            this.requires2FA = true;
            this.username = res.username;
            this.message = '';
          }

          else if (res.token) {
            this.authService.saveTokens(res.token, res.refreshToken);
            this.redirectUserByRole(googleUser.name);
          }
        },
        error: (err) => {
          this.isGoogleProcessing = false;
          Swal.fire({
            icon: 'error',
            title: 'Login Failed',
            text: err.error?.message || 'Unable to sign in with Google.',
            heightAuto: false
          });
        }
      });
    });
  }

  login() {
    this.authService.login(this.username, this.password).subscribe({
      next: (res) => {
        this.logger.info('Login response:', res);

        // NẾU BACKEND YÊU CẦU 2FA
        if (res.requires2FA) {
          this.requires2FA = true;
          this.message = '';
        }
        else if (res.token) {
          this.authService.setPassword(this.password);
          this.redirectUserByRole();
        }
      },
      error: (err: HttpErrorResponse) => {
        let title = 'Login Error';
        let text = 'Unable to sign in. Please try again later.';

        if (err.status === 400 || err.status === 401 || err.status === 402) {
          text = 'Incorrect username or password!';
        }

        Swal.fire({
          icon: 'error',
          title: title,
          text: text,
          timer: 2500,
          showConfirmButton: false,
          heightAuto: false,
          allowOutsideClick: false,
          didClose: () => {
            this.message = text;
            if (this.passwordInput) this.passwordInput.nativeElement.focus();
          },
        });
      },
    });
  }

  // HÀM XỬ LÝ NHẬP MÃ 6 SỐ
  verify2FALogin() {
    this.authService.login2FA(this.username, this.otpCode).subscribe({
      next: (res: any) => {
        if (res.token) {
          this.authService.setPassword(this.password);
          this.redirectUserByRole();
        }
      },
      error: (err) => {
        this.otpCode = '';
        Swal.fire({
          icon: 'error',
          title: 'Verification Failed',
          text: 'Invalid or expired 6-digit code. Please try again.',
          heightAuto: false,
          customClass: { popup: 'small-swal' }
        });
      }
    });
  }

  // Hàm phụ trợ để điều hướng user (tránh lặp code)
  private redirectUserByRole(displayName?: string) {
    const role = this.authService.getUserRole();
    const nameToDisplay = displayName || this.username;

    Swal.fire({
      icon: 'success',
      title: 'Login Successful',
      text: `Welcome back, ${nameToDisplay}!`,
      timer: 1500,
      heightAuto: false,
      showConfirmButton: false
    }).then(() => {
      if (role === 'Admin') {
        this.router.navigate(['/admin-page']);
      } else if (role === 'Staff') {
        this.router.navigate(['/product']);
      } else {
        this.router.navigate(['/']);
      }
    });
  }

  goToRegister() {
    this.router.navigate(['/register']);
  }

  loginWithFacebook() {
    Swal.fire({
      icon: 'info',
      title: 'Coming Soon',
      text: 'The Facebook login feature is under development!',
      heightAuto: false
    });
  }
}
