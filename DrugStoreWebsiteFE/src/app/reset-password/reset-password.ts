import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../Services/auth.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [FormsModule, RouterModule, CommonModule],
  templateUrl: './reset-password.html',
  styleUrls: ['../login/login.css']
})
export class ResetPassword implements OnInit {
  email: string = '';
  token: string = '';
  newPassword = '';
  confirmNewPassword = '';
  isLoading = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) { }

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.email = params['email'] || '';
      this.token = params['token'] || '';

      if (!this.email || !this.token) {
        Swal.fire({
          icon: 'error',
          title: 'Link is not valid',
          text: 'The password recovery link is invalid or has expired!',
          heightAuto: false
        }).then(() => {
          this.router.navigate(['/login']);
        });
      }
    });
  }

  onSubmit() {
    if (this.newPassword !== this.confirmNewPassword) {
      Swal.fire('Error', 'The verification password does not match!', 'warning');
      return;
    }

    this.isLoading = true;

    this.authService.resetPassword(this.email, this.token, this.newPassword, this.confirmNewPassword).subscribe({
      next: () => {
        this.isLoading = false;
        Swal.fire({
          icon: 'success',
          title: 'Success!',
          text: 'Your password has been reset. Please log in using your new password.',
          confirmButtonText: 'Login now',
          heightAuto: false
        }).then(() => {
          this.router.navigate(['/login']);
        });
      },
      error: (err) => {
        this.isLoading = false;
        let errorMessage = 'An error occurred; the token may have expired or be invalid.';
        if (typeof err.error === 'string') {
          errorMessage = err.error;
        } else if (err.error?.message) {
          errorMessage = err.error.message;
        }

        Swal.fire({
          icon: 'error',
          heightAuto: false,
          title: 'Error',
          text: errorMessage,
          
        });
      }
    });
  }
}
