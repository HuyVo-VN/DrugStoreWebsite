import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import Swal from 'sweetalert2';
import { AuthService } from '../Services/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './home.html',
  styleUrls: ['./home.css']
})
export class Home implements OnInit {
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
