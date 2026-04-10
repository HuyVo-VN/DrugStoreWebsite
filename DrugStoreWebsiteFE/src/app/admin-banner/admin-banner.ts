import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';
import { BannerService } from '../Services/banner.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-admin-banner',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './admin-banner.html',
  styleUrl: './admin-banner.css'
})
export class AdminBanner implements OnInit {
  private readonly baseUrl = `${environment.dataApiUrl}`;
  private readonly defaultImage = '/images/default-banner.jpg';

  banners: any[] = [];
  loading = false;

  constructor(private bannerService: BannerService) { }

  ngOnInit() {
    this.loadBanners();
  }

  loadBanners() {
    this.loading = true;
    this.bannerService.getAllBanners().subscribe({
      next: (res) => {
        if (res.isSuccess && res.value) {
          this.banners = res.value;
        } else if (res.data) {
          this.banners = res.data;
        }
        this.loading = false;
      },
      error: (err) => {
        Swal.fire('Error', 'Failed to load banners', 'error');
        this.loading = false;
      }
    });
  }

  getBannerImageUrl(imageUrl: string | null): string {
    if (!imageUrl) return this.defaultImage;
    return imageUrl.startsWith('http') ? imageUrl : `${this.baseUrl}${imageUrl}`;
  }

  openAddBannerModal() {
    Swal.fire({
      title: '<h2 style="font-size: 24px; font-weight: bold; margin: 0;">Add new banner</h2>',
      heightAuto: false,
      html:
        `<style>
          .swal-form-container label { font-weight: bold; margin-top: 10px; display: block; text-align: left; font-size: 14px; }
          .swal2-input, .swal2-file { width: 100% !important; box-sizing: border-box !important; margin: 0 !important; font-size: 14px; }
          .img-wrapper { margin-top: 10px; text-align: center; }
          .swal-image-preview { width: 100%; height: 150px; object-fit: cover; border: 1px dashed #ccc; border-radius: 5px; }
        </style>
        <div class="swal-form-container">
          <label>Title</label>
          <input id="swal-title" class="swal2-input" placeholder="Summer sale">
          
          <label>Target URL</label>
          <input id="swal-link" class="swal2-input" placeholder="/?collection=sale">
          
          <label>Position</label>
          <select id="swal-order" class="swal2-select" style="width: 100%; margin: 0; padding: 6px 8px; font-size: 14px; height: 38px;">
            <optgroup label="Main Slider (Large image slides to the left)">
              <option value="1">Main Slider - Image 1</option>
              <option value="2">Main Slider - Image 2</option>
              <option value="3">Main Slider - Image 3</option>
              <option value="4">Main Slider - Image 4</option>
              <option value="5">Main Slider - Image 5</option>
            </optgroup>
            <optgroup label="Side Banners (2 small images in the right corner)">
              <option value="11">Side Banner - TOP</option>
              <option value="12">Side Banner - BOTTOM</option>
            </optgroup>
          </select>
          
          <label>Banner Image</label>
          <input id="swal-image-file" type="file" class="swal2-file" accept="image/*">
          
          <div class="img-wrapper">
            <img id="swal-image-preview" class="swal-image-preview" src="${this.defaultImage}">
          </div>
        </div>`,
      showCancelButton: true,
      confirmButtonText: 'Create new',
      confirmButtonColor: '#3792b3',
      didOpen: () => {
        const fileInput = document.getElementById('swal-image-file') as HTMLInputElement;
        const preview = document.getElementById('swal-image-preview') as HTMLImageElement;
        fileInput.onchange = () => {
          if (fileInput.files && fileInput.files[0]) {
            preview.src = URL.createObjectURL(fileInput.files[0]);
          }
        };
      },
      preConfirm: () => {
        const title = (document.getElementById('swal-title') as HTMLInputElement).value;
        if (!title) {
          Swal.showValidationMessage(`Please input the title`);
          return false;
        }
        return {
          title: title,
          targetUrl: (document.getElementById('swal-link') as HTMLInputElement).value,
          displayOrder: (document.getElementById('swal-order') as HTMLInputElement).value,
          imageFile: (document.getElementById('swal-image-file') as HTMLInputElement).files?.[0]
        };
      }
    }).then((result) => {
      if (result.isConfirmed) {
        const data = result.value;
        const formData = new FormData();
        formData.append('Title', data.title);
        formData.append('TargetUrl', data.targetUrl || '');
        formData.append('DisplayOrder', data.displayOrder || '0');
        if (data.imageFile) formData.append('ImageFile', data.imageFile);

        this.bannerService.createBanner(formData).subscribe({
          next: () => {
            Swal.fire('Success', 'Banner created.', 'success');
            this.loadBanners();
          },
          error: (err) => Swal.fire('Fail', err.error?.message || 'Error when create!', 'error')
        });
      }
    });
  }

  deleteBanner(banner: any) {
    Swal.fire({
      title: 'Delete Banner?',
      heightAuto: false,
      text: `Are you sure to delete banner: ${banner.title}?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Delete'
    }).then((result) => {
      if (result.isConfirmed) {
        this.bannerService.deleteBanner(banner.id).subscribe({
          next: () => {
            Swal.fire({
              title: 'Deleted!',
              text: 'Banner deleted.',
              icon: 'success',
              heightAuto: false,
              backdrop: true
            });
            this.loadBanners();
          },
          error: (err) => Swal.fire('Error', 'Can not delete banner!', 'error')
        });
      }
    });
  }

  toggleStatus(banner: any) {
    const newStatus = !banner.isActive;
    const formData = new FormData();
    formData.append('Title', banner.title);
    formData.append('TargetUrl', banner.targetUrl || '');
    formData.append('DisplayOrder', banner.displayOrder.toString());
    formData.append('IsActive', newStatus.toString());

    this.bannerService.updateBanner(banner.id, formData).subscribe({
      next: () => {
        banner.isActive = newStatus;
        Swal.fire({ icon: 'success', title: 'Update status successfully', timer: 1200, showConfirmButton: false });
      },
      error: () => Swal.fire('Error', 'Can not update status!', 'error')
    });
  }
}
