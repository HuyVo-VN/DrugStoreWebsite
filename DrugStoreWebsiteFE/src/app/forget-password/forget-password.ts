import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../Services/auth.service';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';
import { UserService } from '../Services/user';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-forget-password',
  imports: [FormsModule, CommonModule],
  templateUrl: './forget-password.html',
  styleUrl: './forget-password.css'
})
export class ForgetPassword implements OnInit {
  email!: string;
  username: string = '';

  constructor(private route: ActivatedRoute, private router: Router, private authService: AuthService, private userService: UserService) { }

  ngOnInit() {
    this.authService.username$.subscribe(name => {
      this.username = name;
    });
  }
  forgetPassword() {
    const token = this.authService.getAccessToken();

    this.authService.forgetPassword(this.email)
      .subscribe({
        next: (res: any) => {
          if (res.status === 404 || !res.data) {
            Swal.fire({
              icon: 'error',
              title: 'User Not Found',
              text: res.message || 'No account found with this email.',
              showConfirmButton: true,
              heightAuto: false,
              customClass: { popup: 'small-swal' }
            });
            return;
          }

          let link = res.data;
 
          Swal.fire({
            icon: 'success',
            title: 'Forget Password',
            html: `
            Click <a href="#" id="resetLink" style="color:#007bff; text-decoration:underline;">here</a> to reset your password.
          `,
            showCancelButton: true,
            showConfirmButton: false,
            heightAuto: false,
            customClass: { popup: 'small-swal' },
            didOpen: () => {
              const resetLink = document.getElementById('resetLink');
              if (resetLink) {
                resetLink.addEventListener('click', (e) => {
                  e.preventDefault();
                  window.location.href = link;
                });
              }
            }
          });
        },
        error: (err) => {
          let errorMessage = 'Invalid email address!';
          if (err.status === 400 && err.error && err.error.errors) {
            const messages = Object.values(err.error.errors).flat();
            if (messages.length > 0) {
              errorMessage = messages.join('\n');
            }
          }
          Swal.fire({
            icon: 'error',
            title: 'Error',
            text: errorMessage,
            showConfirmButton: true,
            heightAuto: false,
            customClass: { popup: 'small-swal' }
          });
        }
      });
  }

  goBack() {
    window.history.back();
  }
}
