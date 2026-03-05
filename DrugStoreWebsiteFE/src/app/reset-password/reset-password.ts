import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../Services/auth.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css'
})
export class ResetPassword implements OnInit {
  email: string = '';
  token: string = '';
  newPassword: string = '';
  newConfirmPassword: string = '';
  errorMessage: string = '';

  constructor(private route: ActivatedRoute, private router: Router, private authService: AuthService) { }

  ngOnInit() {
    const urlParams = new URLSearchParams(window.location.search);
    const rawEmail = urlParams.get('email');
    const rawToken = urlParams.get('token');

    if (rawEmail && rawToken) {
      this.email = rawEmail.trim();
      this.token = rawToken.trim();
    } else {
      this.errorMessage = 'Invalid link (missing email/token).';
      Swal.fire({
        icon: 'error',
        title: 'Invalid Link',
        text: this.errorMessage,
        showConfirmButton: true,
        heightAuto: false,
        customClass: {
          popup: 'small-swal'
        }
      });
    }
  }

  resetPassword() {
    this.errorMessage = '';

    if (this.newPassword !== this.newConfirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      Swal.fire({
        icon: 'warning',
        title: 'Mismatch',
        text: this.errorMessage,
        showConfirmButton: true,
        heightAuto: false,
        customClass: {
          popup: 'small-swal'
        }
      });
      return;
    }

    if (!this.email || !this.token) {
      this.errorMessage = 'Missing email or token information.';
      Swal.fire({
        icon: 'error',
        title: 'Missing Info',
        text: this.errorMessage,
        timer: 4000,
        showConfirmButton: true,
        heightAuto: false,
        customClass: {
          popup: 'small-swal'
        }
      });
      return;
    }

    this.authService.resetPassword(this.email, this.token, this.newPassword, this.newConfirmPassword)
      .subscribe({
        next: () => {
          Swal.fire({
            icon: 'success',
            title: 'Success',
            text: 'Your password has been changed successfully!',
            showConfirmButton: true,
            heightAuto: false,
            customClass: {
              popup: 'small-swal'
            },
            didClose: () => {
              this.router.navigate(['/login']);
            }
          });
        },
        error: (err) => {
          if (err.status === 400 && err.error && Array.isArray(err.error)) {
            this.errorMessage = err.error.map((e: any) => e.description).join('; ');
          } else {
            let errorDetail = 'Unknown error.';
            if (err.error?.message) {
              errorDetail = err.error.message;
            } else if (err.statusText) {
              errorDetail = err.statusText;
            }

            this.errorMessage = `
              The password must contain at least 6 characters, including:<br>
              • At least one uppercase letter (A-Z)<br>
              • At least one lowercase letter (a-z)<br>
              • At least one number (0-9)<br>
              • At least one special character (!@#$...)
            `;

            Swal.fire({
              icon: 'error',
              title: 'Failed',
              text: 'Invalid password format.',
              showConfirmButton: true,
              heightAuto: false,
              customClass: {
                popup: 'small-swal'
              }
            });
          }
        }
      });
  }
  goBack() {
    window.history.back();
  }
}
