import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../Services/auth.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-forget-password',
  standalone: true,
  imports: [FormsModule, RouterModule, CommonModule],
  templateUrl: './forget-password.html',
  styleUrls: ['../login/login.css']
})
export class ForgetPassword {
  email = '';
  isLoading = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) { }

  onSubmit() {
    if (!this.email) {
      Swal.fire('Error', 'Please enter your email address!', 'warning');
      return;
    }

    this.isLoading = true;

    Swal.fire({
      title: 'Sending Email...',
      text: 'Please wait a moment.',
      allowOutsideClick: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });

    this.authService.forgetPassword(this.email).subscribe({
      next: (res: any) => {
        this.isLoading = false;
        Swal.fire({
          icon: 'success',
          title: 'Link sent!',
          heightAuto: false,
          text: 'Please check your email inbox (including your Spam folder) to reset your password.',
          confirmButtonText: 'Go back to Login'
        }).then(() => {
          this.router.navigate(['/login']);
        });
      },
      error: (err) => {
        this.isLoading = false;
        Swal.fire({
          icon: 'error',
          title: 'Error',
          heightAuto: false,
          text: err.error?.message || 'No accounts were found with this email address!',
        });
      }
    });
  }
}
