import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../Services/auth.service';
import { UserService } from '../Services/user';
import { AppRoles } from '../enums/role.enums';

@Component({
  selector: 'app-footer',
  imports: [RouterModule, CommonModule],
  templateUrl: './footer.html',
  styleUrl: './footer.css'
})
export class Footer implements OnInit {

  userRole = '';
  currentUrl = '';

  public AppRoles = AppRoles;

  constructor(
    private authService: AuthService,
    private router: Router,
    private userService: UserService
  ) { }

  ngOnInit() {
    this.authService.role$.subscribe((role) => {
      this.userRole = role;
    });
    this.router.events.subscribe(() => {
      this.currentUrl = this.router.url;
    });
  }

  public get isCustomer(): boolean {
    return this.userRole === this.AppRoles.Customer || this.userRole === '';
  }

  get isDisplay() {
    if (this.currentUrl === '/login' || this.currentUrl.startsWith('/reset-password') || this.currentUrl === '/forget-password'
      || this.currentUrl.startsWith('/change-password') || this.currentUrl === '/register')
      return false;
    return true;
  }

  get displayFooter() {
    if (this.isCustomer && this.isDisplay)
      return true;
    return false;
  }
}