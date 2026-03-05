import { Component, ViewChild, ElementRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../Services/auth.service';
import Swal from 'sweetalert2';
import { LoggerService } from '../Services/logger.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
})
export class Login {
  username = '';
  password = '';
  message = '';

  @ViewChild('passwordInput') passwordInput!: ElementRef;

  constructor(private http: HttpClient, private router: Router, private authService: AuthService, private logger: LoggerService) { }

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
              this.router.navigate(['/user']);
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
}
