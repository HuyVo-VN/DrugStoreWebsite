import { Component } from '@angular/core';
import { Footer } from '../footer/footer';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { Header } from '../header/header';
import { AuthService } from '../Services/auth.service';

@Component({
  selector: 'app-admin-page',
  imports: [],
  templateUrl: './admin-page.html',
  styleUrl: './admin-page.css'
})
export class AdminPage {
  username = '';
  userRole = '';

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit() {
    this.authService.username$.subscribe(name => {
      this.username = name;
    });
    this.userRole = this.authService.getUserRole();
  }

}
