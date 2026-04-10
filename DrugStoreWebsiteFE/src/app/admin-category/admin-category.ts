import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CategoryService } from '../Services/category.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-admin-category',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-category.html',
  styleUrl: './admin-category.css'
})
export class AdminCategory implements OnInit {
  categories: any[] = [];
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  showModal = false;
  isEditMode = false;
  currentCategoryId: string | null = null;
  formData = { name: '', description: '' };

  constructor(private categoryService: CategoryService) { }

  ngOnInit() {
    this.loadCategories();
  }

  loadCategories() {
    this.categoryService.getCategoriesPaged(this.pageNumber, this.pageSize).subscribe({
      next: (res) => {
        if (res.status === 200) {
          this.categories = res.data.items;
          this.totalCount = res.data.totalCount;
          this.totalPages = Math.ceil(this.totalCount / this.pageSize);
        }
      },
      error: (err) => console.error('Lỗi tải danh mục:', err)
    });
  }

  openAddModal() {
    this.isEditMode = false;
    this.currentCategoryId = null;
    this.formData = { name: '', description: '' };
    this.showModal = true;
  }

  openEditModal(category: any) {
    this.isEditMode = true;
    this.currentCategoryId = category.id;
    this.formData = { name: category.name, description: category.description };
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
  }

  saveCategory() {
    if (!this.formData.name.trim()) {
      Swal.fire('Warning', 'The category name cannot be left blank!', 'warning');
      return;
    }

    if (this.isEditMode && this.currentCategoryId) {
      this.categoryService.updateCategory(this.currentCategoryId, this.formData).subscribe({
        next: () => {
          Swal.fire('Success', 'The catalog has been updated!', 'success');
          this.closeModal();
          this.loadCategories();
        },
        error: (err) => Swal.fire('Error', err.error?.message || 'Unable to update', 'error')
      });
    } else {
      this.categoryService.createCategory(this.formData).subscribe({
        next: () => {
          Swal.fire('Success', 'New category added!', 'success');
          this.closeModal();
          this.loadCategories();
        },
        error: (err) => Swal.fire('Error', err.error?.message || 'Cannot create new', 'error')
      });
    }
  }

  deleteCategory(id: string) {
    Swal.fire({
      title: 'Are you sure?',
      heightAuto: false,
      text: "You will not be able to restore this category!",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
      if (result.isConfirmed) {
        this.categoryService.deleteCategory(id).subscribe({
          next: () => {
            Swal.fire('Deleted!', 'The category has been deleted.', 'success');
            this.loadCategories();
          },
          error: (err) => Swal.fire('Error', err.error?.message || 'Cannot be deleted', 'error')
        });
      }
    });
  }

  toggleStatus(category: any) {
    const newStatus = !category.isActive;
    this.categoryService.updateStatus(category.id, newStatus).subscribe({
      next: () => {
        category.isActive = newStatus;
        Swal.fire({
          toast: true, position: 'top-end', showConfirmButton: false,
          timer: 1500, icon: 'success', title: '', heightAuto: false,
        });
      },
      error: () => Swal.fire('Lỗi', 'Unable to update status', 'error')
    });
  }

  changePage(newPage: number) {
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.pageNumber = newPage;
      this.loadCategories();
    }
  }
}
