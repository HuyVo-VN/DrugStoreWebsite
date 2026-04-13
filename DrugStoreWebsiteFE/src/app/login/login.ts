import { Component, ViewChild, ElementRef, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
  imports: [FormsModule, RouterModule, GoogleSigninButtonModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
})
export class Login {
  username = '';
  password = '';
  message = '';

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

      if (!googleUser || !googleUser.idToken) {
        return; 
      }

      this.authService.googleLogin(googleUser.idToken).subscribe({
        next: (res) => {
          if (res.token) {
            this.authService.saveTokens(res.token, res.refreshToken);

            const role = this.authService.getUserRole();

            Swal.fire({
              icon: 'success',
              title: 'Login Successful',
              text: `Welcome back, ${googleUser.name}!`,
              timer: 1500,
              heightAuto: false,
              showConfirmButton: false
            }).then(() => {
              if (role === 'Admin') this.router.navigate(['/admin-page']);
              else if (role === 'Staff') this.router.navigate(['/product']);
              else this.router.navigate(['/']);
            });
          }
        },
        error: (err) => {
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
    // Call login API via AuthService
    this.authService.login(this.username, this.password).subscribe({
      next: (res) => {
        this.logger.info('Login response:', res);

        // If login is successful and token is returned
        if (res.token) {
          this.authService.setPassword(this.password);
          const role = this.authService.getUserRole();

          Swal.fire({
            icon: 'success',
            title: 'Login Successful',
            timer: 1500,
            heightAuto: false,
            showConfirmButton: false
          }).then(() => {
            // Redirect user based on their role
            if (role === 'Admin') {
              this.router.navigate(['/admin-page']);
            } else if (role === 'Staff') {
              this.router.navigate(['/product']);
            }else
            {
              this.router.navigate(['/']);
            }
          });
        } else {
          Swal.fire({
          icon: 'error',
          title: 'Login Failed',
          text: 'Incorrect username or password!',
          timer: 2500,
          showConfirmButton: false,
          heightAuto: false,
          allowOutsideClick: false,
          didClose: () => {
            this.passwordInput.nativeElement.focus();
          }
        });
        }
      },

      error: (err: HttpErrorResponse) => {
        let title = 'Login Error';
        let text = 'Unable to sign in. Please try again later.';

        if (err.status === 400 || err.status === 402) {
          title = 'Login Error';
          text = 'Incorect username or password!';
        }

        // Handle server or network error
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
            this.passwordInput.nativeElement.focus();
          },
        });
      },
  });
  }

  // Navigate to register page
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
