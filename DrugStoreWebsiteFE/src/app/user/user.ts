import { Component, OnInit } from '@angular/core';
import { UserService } from '../Services/user';
import Swal from 'sweetalert2';
import { CommonModule, DatePipe } from '@angular/common';
import { AuthService } from '../Services/auth.service';

@Component({
  selector: 'app-user',
  imports: [CommonModule, DatePipe],
  templateUrl: './user.html',
  styleUrls: ['./user.css']
})
export class User implements OnInit {
  users: any[] = [];
  allRoles: string[] = ['Admin', 'Staff', 'Customer'];
  currentUserId: string | null = null;

  constructor(private userService: UserService, private authService: AuthService) { }

  ngOnInit() {
    this.getUsers();
    this.currentUserId=this.authService.getUserId();
  }

  getUsers() {
    this.userService.getUsers().subscribe({
      next: (res) => {
        if (res.status === 200) {
          this.users = res.data;
        } else {
          Swal.fire({
            title: 'Notice',
            text: 'No users found',
            icon: 'info',
            heightAuto: false
          });
        }
      },
      error: () => Swal.fire({
        title: 'Error',
        text: 'Failed to fetch data',
        icon: 'error',
        heightAuto: false
      })
    });
  }
  assignRole(user: any) {
    // Filter out roles that the user already has
    const availableRoles = this.allRoles.filter((role) => !user.roles?.includes(role));

    // Check if there are available roles to assign
    if (availableRoles.length === 0) {
      availableRoles.push('No available roles');
    }

    Swal.fire({
      title: `Assign role for ${user.fullName}`,
      input: 'select',
      inputOptions: availableRoles.reduce((acc, role) => {
        acc[role] = role;
        return acc;
      }, {} as Record<string, string>),
      inputPlaceholder: 'Select a role',
      showCancelButton: true,
      confirmButtonText: 'Save',
      cancelButtonText: 'Back',
      heightAuto: false,
      preConfirm: (selectedRole) => {
        if (!selectedRole) {
          Swal.showValidationMessage('Please select a role!');
        }
        return selectedRole;
      },
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        const role = result.value;
        this.userService.assignRole(user.id, role).subscribe({
          next: () => {
            Swal.fire({
              title: 'Success',
              text: 'Role assigned successfully',
              icon: 'success',
              heightAuto: false,
            });
            user.roles = [role];
          },
          error: () =>
            Swal.fire({
              title: 'Error',
              text: 'Failed to assign role',
              icon: 'error',
              heightAuto: false,
            }),
        });
      }
    });
  }
  delete(user: any) {
    Swal.fire({
      title: 'Are you sure?',
      html: `Do you want to delete user: <strong>${user.fullName}</strong>?<br>This action cannot be undone.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, delete it!',
      cancelButtonText: 'Cancel',
      heightAuto: false
    }).then((result) => {
      if (result.isConfirmed) {
        this.userService.deleteUser(user.id)
          .subscribe({
            next: () => {
              Swal.fire({
                icon: 'success',
                title: 'Success',
                text: 'User has been deleted successfully!',
                showConfirmButton: true,
                heightAuto: false,
                customClass: {
                  popup: 'small-swal'
                }
              });
              this.users = this.users.filter(u => u.id !== user.id);

            },
            error: (err) => {
              Swal.fire({
                icon: 'error',
                title: 'Failed',
                text: err.message,
                showConfirmButton: true,
                heightAuto: false,
                customClass: {
                  popup: 'small-swal'
                }
              });
            }

          });
      }
    }
    )
  }

  goBack() {
    window.history.back();
  }
}
