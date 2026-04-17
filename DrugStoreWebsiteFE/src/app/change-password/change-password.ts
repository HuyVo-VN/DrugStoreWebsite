import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../Services/auth.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import Swal from 'sweetalert2';
import { UserService } from '../Services/user';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './change-password.html',
  styleUrl: './change-password.css'
})
export class ChangePassword implements OnInit {
  oldPassword = '';
  newPassword = '';
  newConfirmPassword = '';
  errorMessage = '';

  // UX: Đo độ mạnh mật khẩu
  passwordStrength = 0;
  strengthText = '';
  strengthColor = '#ccc';

  // 2FA: Trạng thái bảo mật 2 lớp (Dành cho phần sau)
  is2FAEnabled = false;
  show2FASetup = false;

  qrCodeBase64 = '';
  otpCode = '';

  constructor(
    private router: Router,
    private authService: AuthService,
    private userService: UserService
  ) { }

  ngOnInit() {
    // 1. Lấy username hiện tại đang đăng nhập từ AuthService
    const currentUsername = this.authService.getUsername();

    if (currentUsername) {
      // 2. Gọi API lấy thông tin User để check cờ 2FA
      this.userService.getUserByUsername(currentUsername).subscribe({
        next: (res: any) => {
          // Backend .NET của sếp thường bọc data trong res.data
          const userProfile = res.data;

          // 3. Cập nhật trạng thái hiển thị
          if (userProfile && userProfile.twoFactorEnabled !== undefined) {
            this.is2FAEnabled = userProfile.twoFactorEnabled;
          }
        },
        error: (err) => {
          console.error('Không lấy được profile user:', err);
        }
      });
    }
  }

  checkPasswordStrength() {
    let strength = 0;
    if (this.newPassword.length >= 6) strength += 1;
    if (this.newPassword.match(/[A-Z]/)) strength += 1; // Có chữ hoa
    if (this.newPassword.match(/[a-z]/)) strength += 1; // Có chữ thường
    if (this.newPassword.match(/[0-9]/)) strength += 1; // Có số
    if (this.newPassword.match(/[\W_]/)) strength += 1; // Có ký tự đặc biệt (!@#$)

    this.passwordStrength = strength;

    switch (strength) {
      case 0: case 1: case 2:
        this.strengthText = 'Weak';
        this.strengthColor = '#ff4d4d'; // Đỏ
        break;
      case 3: case 4:
        this.strengthText = 'Medium';
        this.strengthColor = '#ffcc00'; // Vàng
        break;
      case 5:
        this.strengthText = 'Strong';
        this.strengthColor = '#00cc44'; // Xanh lá
        break;
      default:
        this.strengthText = '';
        this.strengthColor = '#ccc';
    }
  }

  changePassword() {
    this.errorMessage = '';

    if (this.newPassword !== this.newConfirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    if (this.passwordStrength < 3) {
      this.errorMessage = 'New password is too weak. Please include letters, numbers, and special characters.';
      return;
    }

    // 🚀 GỌI API ĐỔI MẬT KHẨU THẬT SỰ
    this.authService.changePassword(this.oldPassword, this.newPassword).subscribe({
      next: () => {
        Swal.fire({
          icon: 'success',
          title: 'Success',
          text: 'Your password has been changed successfully! Please log in again.',
          heightAuto: false,
          customClass: { popup: 'small-swal' },
          showConfirmButton: true
        }).then(() => {
          // Chuẩn bảo mật: Đổi pass xong phải xóa token cũ và bắt đăng nhập lại
          this.authService.logout();
        });
      },
      error: (err) => {
        // Bắt lỗi từ Backend (ví dụ: Sai pass cũ)
        this.errorMessage = err.error?.message || 'Incorrect old password or server error.';
        Swal.fire({
          icon: 'error',
          title: 'Failed',
          text: this.errorMessage,
          heightAuto: false,
          customClass: { popup: 'small-swal' }
        });
      }
    });
  }

  goBack() {
    window.history.back();
  }

  // 1. Gọi API lấy hình QR
  toggle2FASetup() {
    this.show2FASetup = !this.show2FASetup;
    if (this.show2FASetup) {
      this.authService.setup2FA().subscribe({
        next: (res: any) => {
          this.qrCodeBase64 = res.qrCodeImage; // Hình ảnh Base64 từ Backend
        }
      });
    }
  }

  // 2. Gửi mã 6 số để kích hoạt chính thức
  confirm2FASetup() {
    this.authService.verify2FASetup(this.otpCode).subscribe({
      next: (res: any) => {
        Swal.fire('Thành công', 'Đã bật bảo mật 2 lớp!', 'success');
        this.is2FAEnabled = true;
        this.show2FASetup = false;
      },
      error: (err) => {
        Swal.fire('Lỗi', 'Mã OTP không đúng hoặc đã hết hạn', 'error');
      }
    });
  }

  disable2FA() {
    Swal.fire({
      title: 'Are you sure?',
      text: "You are turning off extra security for your account!",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes, disable it!',
      heightAuto: false
    }).then((result) => {
      if (result.isConfirmed) {
        // Gọi API xuống Backend
        this.authService.disable2FA().subscribe({
          next: () => {
            // Tắt thành công thì đổi cờ để HTML tự động ẩn giao diện
            this.is2FAEnabled = false;
            this.show2FASetup = false;
            Swal.fire({
              title: 'Disabled!',
              text: '2FA has been turned off.',
              icon: 'success',
              heightAuto: false
            });
          },
          error: (err) => {
            Swal.fire('Error', 'Could not disable 2FA', 'error');
          }
        });
      }
    });
  }
}
