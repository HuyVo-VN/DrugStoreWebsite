import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../Services/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class Register {
  model: any = {
    gender: '',
    password: '',
    confirmPassword: ''
  };

  imagePath: string = 'images/bg.png';

  constructor(private authService: AuthService, private router: Router) { }

  onSubmit() {
    if (this.model.password !== this.model.confirmPassword) {
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'Passwords do not match!',
        heightAuto: false,
        customClass: { popup: 'medium-swal' }
      });
      return;
    }

    const payload = { ...this.model };

    delete payload.confirmPassword;

    this.authService.register(this.model).subscribe({
      next: () => {
        Swal.fire({
          icon: 'success',
          title: 'Registration Successful',
          text: 'Your account has been created successfully!',
          timer: 2500,
          showConfirmButton: false,
          heightAuto: false,
          customClass: {
            popup: 'medium-swal'
          },
          didClose: () => {
            this.router.navigate(['/login']);
          }
        });
      },
      error: (err) => {
        Swal.fire({
          icon: 'error',
          title: 'Registration Failed',
          text: 'Something went wrong. Please try again.',
          showConfirmButton: true,
          heightAuto: false,
          customClass: {
            popup: 'medium-swal'
          }
        });
      }
    });
  }
}
