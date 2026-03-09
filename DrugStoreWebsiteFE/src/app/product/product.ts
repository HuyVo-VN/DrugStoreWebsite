import { Component, OnInit } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ProductService } from '../Services/product.service';
import { CategoryService } from '../Services/category.service';
import Swal from 'sweetalert2';
import { AuthService } from '../Services/auth.service';
import { LoggerService } from '../Services/logger.service';
import { FormsModule } from '@angular/forms';
import { debounceTime } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-product',
  standalone: true,
  imports: [CommonModule, RouterModule, CurrencyPipe, FormsModule, MatFormFieldModule, MatSelectModule, MatInputModule, MatProgressSpinnerModule],
  templateUrl: './product.html',
  styleUrls: ['./product.css'],
})
export class Product implements OnInit {
  private readonly baseUrl = 'https://localhost:5287';
  private readonly defaultImage = '/images/default-product.png';


  products: any[] = [];
  categories: any[] = [];
  private categoriesMap = new Map<string, string>();
  searchSubject = new Subject<string>();
  currentKeyword: string = '';

  showFilter = false;

  currentPage: number = 1;
  pageSize: number = 10;
  totalPages: number = 0;
  totalCount: number = 0;

  minPrice: number | null = null;
  maxPrice: number | null = null;

  value = '';
  viewValue = '';
  selectedCategoryId = '';

  loading = false;




  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    private authService: AuthService,
    private router: Router,
    private logger: LoggerService
  ) { }

  ngOnInit() {
    this.loadData();
    this.searchSubject.pipe(debounceTime(2000)).subscribe(keyword => {
      this.performSearch(keyword);
    });
  }

  loadData() {
    this.loading = true;
    this.categoryService.getCategories().subscribe({
      next: (res) => {
        if (res.status === 200 && res.data) {
          this.categories = res.data.map((category: any) => ({
            ...category,
            value: category.id,
            viewValue: category.name
          }));
          this.categoriesMap.clear();
          for (const category of res.data) {
            this.categoriesMap.set(category.id, category.name);
          }
        }
        this.loadProducts();
        this.loading = false;

      },
      error: (err) => {
        this.logger.error('Failed to load categories', err);
        this.loadProducts();
      },
    });
  }

  loadProducts() {
    this.productService.getProducts(this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        if (res.status === 200 && res.data) {
          const pagedResult = res.data;

          this.products = pagedResult.items.map((product: any) => {
            return {
              ...product,
              categoryName: this.categoriesMap.get(product.categoryId) || 'N/A',
            };
          });
          this.totalPages = pagedResult.totalPages;
          this.totalCount = pagedResult.totalCount;
        }
      },
      error: () => Swal.fire('Error', 'Failed to fetch products', 'error'),
    });
  }

  //Get picture funtion
  getProductImageUrl(imageUrl: string | null): string {
    if (!imageUrl) {
      return this.defaultImage; // if dont have link --> get default picture
    }
    if (imageUrl.startsWith('http')) {
      return imageUrl; // if have, use
    }
    return `${this.baseUrl}${imageUrl}`;
  }

  openAddProductModal() {
    // Create options for category select
    const categoryOptions = this.categories
      .map((cat) => `<option value="${cat.id}">${cat.name}</option>`)
      .join('');

    Swal.fire({
      title:
        '<h2 style="font-size: 28px; color: #333; font-weight: bold; margin: 0;">Create New Product</h2>',
      html:
        //Style for form
        `<style>
          .swal-form-columns { display: flex; gap: 20px; text-align: left; }
          .swal-form-col { flex: 1; }
          .swal-form-container label { font-weight: bold; margin-top: 10px; margin-bottom: 5px; display: block; font-size: 14px; }
          .swal2-input, .swal2-select, .swal2-textarea, .swal2-file { 
            width: 100% !important; box-sizing: border-box !important; margin: 0 !important; font-size: 14px;
          }
          .swal2-select {
            font-size: 14px !important;
            padding: 6px 8px !important;
            height: 38px !important;
          }
          .swal2-textarea { height: 80px; }
          .swal-image-preview {
            width: 100%; height: 200px; border: 1px dashed #ccc; border-radius: 5px;
            margin-top: 10px; object-fit: contain; background-color: #f9f9f9; display: block;
          }
          .img-wrapper {
          position: relative;
          width: 100%;
          }

          .remove-image-btn {
            position: absolute;
            top: 6px;
            right: 6px;
            background: rgba(0,0,0,0.6);
            color: white;
            border: none;
            width: 16px;
            height: 16px;
            font-size: 18px;
            font-weight: bold;
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: center;
            border-radius: 4px;s
          }
          .remove-image-btn:hover {
            background: rgba(0,0,0,0.8);
          }
        </style>` +
        /* Form container */
        `<div class="swal-form-columns">` +
        /* column 1: Input form */
        `<div class="swal-form-col swal-form-container">` +
        `<label for="swal-name">Name *</label>` +
        `<input id="swal-name" class="swal2-input" placeholder="Product Name">` +
        `<label for="swal-category">Category *</label>` +
        `<select id="swal-category" class="swal2-select">` +
        `<option value="" disabled selected>Select a category</option>` +
        categoryOptions +
        `</select>` +
        `<label for="swal-price">Price *</label>` +
        `<input id="swal-price" type="number" class="swal2-input" placeholder="0">` +
        `<label for="swal-stock">Stock *</label>` +
        `<input id="swal-stock" type="number" class="swal2-input" placeholder="0">` +
        `<label for="swal-discount">Discount Percent (%)</label>` +
        `<input id="swal-discount" type="number" class="swal2-input" placeholder="0" min="0" max="100">` +
        `<div id="swal-flash-sale-fields" style="display: none; background: #fff3cd; padding: 10px; border-radius: 5px; margin-top: 10px;">` +
        `<label for="swal-discount-end" style="color: #856404;">End Date</label>` +
        `<input id="swal-discount-end" type="datetime-local" class="swal2-input" style="margin-top: 5px !important;">` +
        `<label for="swal-sale-stock" style="color: #856404;">Sale Stock</label>` +
        `<input id="swal-sale-stock" type="number" class="swal2-input" placeholder="0" style="margin-top: 5px !important;">` +
        `</div>` +
        `<label for="swal-desc">Description</label>` +
        `<textarea id="swal-desc" class="swal2-textarea" placeholder="Description..."></textarea>` +
        `</div>` +
        /* colunm 2: image upload & preview */
        `<div class="swal-form-col swal-form-container">` +
        `<label for="swal-image-file">Product Image</label>` +
        `<input id="swal-image-file" type="file" class="swal2-file" accept="image/*">` +
        `<label>Preview:</label>` +
        `<div class="img-wrapper">` +
        `<img id="swal-image-preview" class="swal-image-preview" src="" alt="Image preview">` +
        `<button id="btn-remove-image" class="remove-image-btn">&times;</button>` +
        `</div>` +
        `</div>` +
        `</div>`,


      focusConfirm: false,
      showCancelButton: true,
      confirmButtonText: 'Create',
      confirmButtonColor: '#3792b3',
      heightAuto: false,


      customClass: {
        popup: 'swal-form-modal-wide',
      },

      didOpen: () => {
        // Logic Preview picture when chosing file
        const fileInput = document.getElementById('swal-image-file') as HTMLInputElement;
        const preview = document.getElementById('swal-image-preview') as HTMLImageElement;
        const removeBtn = document.getElementById('btn-remove-image') as HTMLButtonElement;

        const toggleRemoveBtn = () => {
          // if default image --> hide delete btn
          if (preview.src.includes('default-product.png')) {
            removeBtn.style.display = 'none';
          } else {
            removeBtn.style.display = 'flex';
          }
        };

        preview.src = this.defaultImage;
        toggleRemoveBtn();

        fileInput.onchange = () => {
          if (fileInput.files && fileInput.files[0]) {
            preview.src = URL.createObjectURL(fileInput.files[0]);
            toggleRemoveBtn();
          }
        };

        removeBtn.onclick = () => {
          preview.src = this.defaultImage;
          fileInput.value = '';
          toggleRemoveBtn();
        };

        const discountInput = document.getElementById('swal-discount') as HTMLInputElement;
        const flashSaleFields = document.getElementById('swal-flash-sale-fields') as HTMLDivElement;

        discountInput.addEventListener('input', () => {
          const val = parseInt(discountInput.value) || 0;
          flashSaleFields.style.display = val > 0 ? 'block' : 'none';
        });

        // Focus to name space
        document.getElementById('swal-name')?.focus();
      },


      preConfirm: () => {
        const name = (document.getElementById('swal-name') as HTMLInputElement).value;
        const categoryId = (document.getElementById('swal-category') as HTMLSelectElement).value;
        const price = (document.getElementById('swal-price') as HTMLInputElement).value;
        const stock = (document.getElementById('swal-stock') as HTMLInputElement).value;
        const discountPercent = (document.getElementById('swal-discount') as HTMLInputElement).value || '0';
        const description = (document.getElementById('swal-desc') as HTMLTextAreaElement).value;
        const discountEndDate = (document.getElementById('swal-discount-end') as HTMLInputElement).value;
        const saleStock = (document.getElementById('swal-sale-stock') as HTMLInputElement).value || '0';

        if (!name || !categoryId || !price || !stock) {
          Swal.showValidationMessage(`Please fill in all required fields (*)`);
          return false;
        }
        return true;
      },
    }).then((result) => {
      if (result.isConfirmed) {
        // Load data and create form data
        const name = (document.getElementById('swal-name') as HTMLInputElement).value;
        const categoryId = (document.getElementById('swal-category') as HTMLSelectElement).value;
        const price = (document.getElementById('swal-price') as HTMLInputElement).value;
        const stock = (document.getElementById('swal-stock') as HTMLInputElement).value;
        const discountPercent = (document.getElementById('swal-discount') as HTMLInputElement).value || '0';
        const description = (document.getElementById('swal-desc') as HTMLTextAreaElement).value;
        const imageFile = (document.getElementById('swal-image-file') as HTMLInputElement)
          .files?.[0];
        const discountEndDate = (document.getElementById('swal-discount-end') as HTMLInputElement).value;
        const saleStock = (document.getElementById('swal-sale-stock') as HTMLInputElement).value || '0';

        const formData = new FormData();
        formData.append('Name', name);
        formData.append('CategoryId', categoryId);
        formData.append('Price', price);
        formData.append('Stock', stock);
        formData.append('DiscountPercent', discountPercent);
        formData.append('Description', description);
        if (imageFile) {
          formData.append('ImageFile', imageFile);
        }
        if (parseInt(discountPercent) > 0) {
          if (discountEndDate) formData.append('DiscountEndDate', discountEndDate);
          formData.append('SaleStock', saleStock);
        }

        // call api service 
        this.productService.createProduct(formData).subscribe({
          next: () => {
            Swal.fire('Created!', 'Product has been created.', 'success');
            this.loadProducts();
          },
          error: (err) => {
            Swal.fire('Failed', err.error?.message || 'Failed to create product.', 'error');
          },
        });
      }
    });
  }

  openEditProductModal(product: any) {
    // Create HTML
    let isImageRemoved = false;
    const categoryOptions = this.categories
      .map(
        (cat) =>
          // Auto selected categoryId
          `<option value="${cat.id}" ${cat.id === product.categoryId ? 'selected' : ''}>
           ${cat.name}
         </option>`
      )
      .join('');

    let currentImgSrc = this.defaultImage;
    if (product.imageUrl && product.imageUrl !== 'null') {
      currentImgSrc = product.imageUrl
        ? (product.imageUrl.startsWith('http')
          ? product.imageUrl
          : `${this.baseUrl}${product.imageUrl}`)
        : '';
    }

    Swal.fire({
      title:
        '<h2 style="font-size: 28px; color: #333; font-weight: bold; margin: 0;">Edit Product</h2>',
      html:

        `<style>
          .swal-form-columns { display: flex; gap: 20px; text-align: left; }
          .swal-form-col { flex: 1; }
          .swal-form-container label { font-weight: bold; margin-top: 10px; margin-bottom: 5px; display: block; font-size: 14px; }
          .swal2-input, .swal2-select, .swal2-textarea, .swal2-file { 
            width: 100% !important; box-sizing: border-box !important; margin: 0 !important; font-size: 14px;
          }
          .swal2-select {
            font-size: 14px !important;
            padding: 6px 8px !important;
            height: 38px !important;
          }
          .swal2-textarea { height: 80px; }
          .swal-image-preview {
            width: 100%; height: 200px; border: 1px dashed #ccc; border-radius: 5px;
            margin-top: 10px; object-fit: contain; background-color: #f9f9f9; display: block;
          }

          .img-wrapper {
            position: relative;
            width: 100%;
          }
          .remove-image-btn {
            position: absolute;
            top: 6px;
            right: 6px;
            background: rgba(0,0,0,0.6);
            color: white;
            border: none;
            width: 16px;
            height: 16px;
            font-size: 18px;
            font-weight: bold;
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: center;
            border-radius: 4px;
          }
          .remove-image-btn:hover {
            background: rgba(0,0,0,0.8);
          }
        </style>` +

        `<div class="swal-form-columns">` +
        //column 1: form re-fill
        `<div class="swal-form-col swal-form-container">` +
        `<label for="swal-name">Name *</label>` +
        `<input id="swal-name" class="swal2-input" value="${product.name}">` +

        `<label for="swal-category">Category *</label>` +
        `<select id="swal-category" class="swal2-select">` +
        categoryOptions +
        `</select>` +

        `<label for="swal-price">Price *</label>` +
        `<input id="swal-price" type="number" class="swal2-input" value="${product.price}">` +

        `<label for="swal-stock">Stock *</label>` +
        `<input id="swal-stock" type="number" class="swal2-input" value="${product.stock}">` +

        `<label for="swal-discount">Discount Percent (%)</label>` +
        `<input id="swal-discount" type="number" class="swal2-input" value="${product.discountPercent || 0}" min="0" max="100">` +
        `<div id="swal-flash-sale-fields" style="display: ${product.discountPercent > 0 ? 'block' : 'none'}; background: #fff3cd; padding: 10px; border-radius: 5px; margin-top: 10px;">` +
        `<label for="swal-discount-end" style="color: #856404;">End Date</label>` +
        `<input id="swal-discount-end" type="datetime-local" class="swal2-input" value="${product.discountEndDate ? product.discountEndDate.substring(0, 16) : ''}" style="margin-top: 5px !important;">` +
        `<label for="swal-sale-stock" style="color: #856404;">Sale Stock</label>` +
        `<input id="swal-sale-stock" type="number" class="swal2-input" value="${product.saleStock || 0}" style="margin-top: 5px !important;">` +
        `</div>` +
        `<label for="swal-desc">Description</label>` +
        `<textarea id="swal-desc" class="swal2-textarea">${product.description || ''}</textarea>` +
        `</div>` +

        //column 2: show the image
        `<div class="swal-form-col swal-form-container">` +
        `<label for="swal-image-file">Change Image</label>` +
        `<input id="swal-image-file" type="file" class="swal2-file" accept="image/*">` +
        `<label>Current/New Image:</label>` +
        `<div class="img-wrapper">` +
        `<img id="swal-image-preview" class="swal-image-preview" src="${currentImgSrc}" alt="No Image">` +
        `<button id="btn-remove-image" class="remove-image-btn">&times;</button>` +
        `</div>` +
        `</div>` +
        `</div>`,


      focusConfirm: false,
      showCancelButton: true,
      confirmButtonText: 'Save Changes',
      confirmButtonColor: '#3792b3',
      heightAuto: false,
      customClass: {
        popup: 'swal-form-modal-wide',
      },
      //Set pointer at the end of Product Name
      didOpen: () => {
        const fileInput = document.getElementById('swal-image-file') as HTMLInputElement;
        const preview = document.getElementById('swal-image-preview') as HTMLImageElement;
        const nameInput = document.getElementById('swal-name') as HTMLInputElement;
        const removeBtn = document.getElementById('btn-remove-image') as HTMLButtonElement;

        const discountInput = document.getElementById('swal-discount') as HTMLInputElement;
        const flashSaleFields = document.getElementById('swal-flash-sale-fields') as HTMLDivElement;

        discountInput.addEventListener('input', () => {
          const val = parseInt(discountInput.value) || 0;
          flashSaleFields.style.display = val > 0 ? 'block' : 'none';
        });

        const toggleRemoveBtn = () => {
          // if default image --> hide delete btn
          if (preview.src.includes('default-product.png')) {
            removeBtn.style.display = 'none';
          } else {
            removeBtn.style.display = 'flex';
          }
        };

        toggleRemoveBtn();

        // Logic Preview new image
        fileInput.onchange = () => {
          if (fileInput.files && fileInput.files[0]) {
            preview.src = URL.createObjectURL(fileInput.files[0]);
            isImageRemoved = false;
            toggleRemoveBtn();
          }
        };

        //remove picture
        removeBtn.onclick = () => {
          preview.src = this.defaultImage;
          fileInput.value = '';
          isImageRemoved = true;
          toggleRemoveBtn();
        };

        // UX: Focus and put pointer at the end of name space
        if (nameInput) {
          nameInput.focus();
          const length = nameInput.value.length;
          nameInput.setSelectionRange(length, length);
        }

      },


      preConfirm: () => {
        // Get new value from form
        const name = (document.getElementById('swal-name') as HTMLInputElement).value;
        const categoryId = (document.getElementById('swal-category') as HTMLSelectElement).value;
        const price = (document.getElementById('swal-price') as HTMLInputElement).value;
        const stock = (document.getElementById('swal-stock') as HTMLInputElement).value;



        if (!name || !categoryId || !price || !stock) {
          Swal.showValidationMessage(`Please fill in all required fields (*)`);
          return false;
        }

        return {
          name, categoryId, price, stock,
          description: (document.getElementById('swal-desc') as HTMLTextAreaElement).value,
          imageFile: (document.getElementById('swal-image-file') as HTMLInputElement).files?.[0],
          isImageRemoved: isImageRemoved
        };
      },
    }).then((result) => {
      if (result.isConfirmed) {
        // Get data and create FormData
        const name = (document.getElementById('swal-name') as HTMLInputElement).value;
        const categoryId = (document.getElementById('swal-category') as HTMLSelectElement).value;
        const price = (document.getElementById('swal-price') as HTMLInputElement).value;
        const stock = (document.getElementById('swal-stock') as HTMLInputElement).value;
        const discountPercent = (document.getElementById('swal-discount') as HTMLInputElement).value || '0';
        const description = (document.getElementById('swal-desc') as HTMLTextAreaElement).value;
        const imageFile = (document.getElementById('swal-image-file') as HTMLInputElement).files?.[0];
        const discountEndDate = (document.getElementById('swal-discount-end') as HTMLInputElement).value;
        const saleStock = (document.getElementById('swal-sale-stock') as HTMLInputElement).value || '0';

        const formData = new FormData();
        formData.append('Name', name);
        formData.append('CategoryId', categoryId);
        formData.append('Price', price);
        formData.append('Stock', stock);
        formData.append('DiscountPercent', discountPercent);
        formData.append('Description', description);
        if (imageFile) {
          formData.append('ImageFile', imageFile);
        }
        if (parseInt(discountPercent) > 0) {
          if (discountEndDate) formData.append('DiscountEndDate', discountEndDate);
          formData.append('SaleStock', saleStock);
        } else {

          formData.append('SaleStock', '0');
        }

        formData.append('DeleteCurrentImage', isImageRemoved ? 'true' : 'false');

        // Call API Update
        this.productService.updateProduct(product.id, formData).subscribe({
          next: () => {
            Swal.fire('Updated!', 'Product has been updated.', 'success');
            this.loadData();
          },
          error: (err) => {
            Swal.fire('Failed', err.error?.message || 'Failed to update product.', 'error');
          }
        });
      }
    });
  }

  delete(product: any) {
    Swal.fire({
      title: 'Are you sure?',
      html: `Do you want to delete product: <strong>${product.name}</strong>?<br>This action cannot be undone.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, delete it!',
      cancelButtonText: 'Cancel',
      heightAuto: false
    }).then((result) => {
      if (result.isConfirmed) {
        this.productService.deleteProduct(product.id)
          .subscribe({
            next: () => {
              Swal.fire({
                icon: 'success',
                title: 'Success',
                text: 'Product has been deleted successfully!',
                showConfirmButton: true,
                heightAuto: false,
                customClass: { popup: 'small-swal' }
              });

              this.products = this.products.filter(u => u.id !== product.id);
            },
            error: (err) => {
              Swal.fire({
                icon: 'error',
                title: 'Failed',
                text: err.message,
                showConfirmButton: true,
                heightAuto: false,
                customClass: { popup: 'small-swal' }
              });
            }
          });
      }
    });
  }

  goBack() {
    window.history.back();
  }

  toggleStatus(event: Event, product: any) {
    event.preventDefault();

    const previousStatus = product.isActive;
    const newStatus = !previousStatus;

    product._loading = true;

    this.productService.updateStatusProduct(product.id, newStatus).subscribe({
      next: () => {
        product.isActive = newStatus;
        product._loading = false;

        Swal.fire({
          icon: 'success',
          title: 'Success',
          text: `Updated to ${newStatus ? 'Active' : 'Inactive'}`,
          showConfirmButton: false,
          timer: 1200
        });
      },
      error: (err) => {
        product._loading = false;

        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: err?.errors || "Update failed",
          showConfirmButton: true
        });
      }
    });
  }


  onSearch(event: any) {
    const keyword = event.target.value;
    this.currentKeyword = keyword;
    this.searchSubject.next(keyword);
  }

  performSearch(keyword: string) {
    this.currentKeyword = keyword;

    if (!keyword.trim()) {
      this.loadProducts();
      return;
    }

    this.productService
      .searchProducts(keyword, this.currentPage, this.pageSize)
      .subscribe({
        next: (res) => {
          const pagedResult = res.value;

          this.products = pagedResult.items.map((product: any) => ({
            ...product,
            categoryName: this.categoriesMap.get(product.categoryId) || 'N/A',
          }));

          this.totalPages = pagedResult.totalPages;
          this.totalCount = pagedResult.totalCount;
        }
      });
  }




  applyFilter() {
    this.toggleFilter();
    if (this.selectedCategoryId) {
      this.productService.filterProducts(this.selectedCategoryId, this.currentPage, this.pageSize).subscribe({
        next: (res) => {
          if (res.value) {
            const pagedResult = res.value;

            this.products = pagedResult.items.map((product: any) => ({
              ...product,
              categoryName: this.categoriesMap.get(product.categoryId) || 'N/A',
            }));

            this.totalPages = pagedResult.totalPages;
            this.totalCount = pagedResult.totalCount;
          }
        },
        error: (err) => {
          Swal.fire({
            icon: 'error',
            title: 'Failed',
            text: err.errors || 'Failed to filter products.',
            heightAuto: false,
          });
        }
      });
    }

  }

  toggleFilter() {
    this.showFilter = !this.showFilter;
  }
  reset() {
    this.minPrice = null;
    this.maxPrice = null;
    this.selectedCategoryId = '';
    this.toggleFilter();
    this.loadData();
  }

  //Next page function 
  onPageChange(page: number) {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }
    this.currentPage = page;

    if (this.currentKeyword && this.currentKeyword.trim() !== '') {
      this.performSearch(this.currentKeyword);
      return;
    }
    if (this.selectedCategoryId) {
      this.toggleFilter();
      this.applyFilter();
      return;

    }

      this.loadProducts();
    
  }

  isSaleActive(product: any): boolean {
    if (!product.discountPercent || product.discountPercent <= 0) return false;
    if (!product.discountEndDate) return false;

    const now = new Date().getTime();
    const endDate = new Date(product.discountEndDate).getTime();

    return endDate > now;
  }

  onCancelSale(productId: string) {
    Swal.fire({
      title: 'Do you want to turn off sales for this product?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Turn off now'
    }).then((result) => {
      if (result.isConfirmed) {
        this.productService.cancelSale(productId).subscribe({
          next: () => {
            Swal.fire('Success', 'Sales turned off!', 'success');
            this.loadProducts(); 
          }
        });
      }
    });
  }

}
