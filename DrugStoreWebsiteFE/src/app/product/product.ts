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
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { ProductForm } from '../product-form/product-form';

@Component({
  selector: 'app-product',
  standalone: true,
  imports: [CommonModule, RouterModule, CurrencyPipe, FormsModule, MatFormFieldModule, MatSelectModule, MatInputModule, MatProgressSpinnerModule, MatDialogModule],
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
    private logger: LoggerService,
    private dialog: MatDialog
  ) { }

  ngOnInit() {
    this.loadData();
    this.searchSubject.pipe(debounceTime(2000)).subscribe(keyword => {
      this.performSearch(keyword);
    });
  }

  loadData() {
    this.loading = true;
    this.categoryService.getAllCategories().subscribe({
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
    const dialogRef = this.dialog.open(ProductForm, {
      width: '850px',
      disableClose: true,
      data: { categories: this.categories, product: null } // Truyền category list sang
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.saveProduct(result, null); // Có dữ liệu thì gọi API
      }
    });
  }

  openEditProductModal(product: any) {
    const dialogRef = this.dialog.open(ProductForm, {
      width: '850px',
      disableClose: true,
      data: { categories: this.categories, product: product } // Truyền cả sản phẩm cũ sang
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.saveProduct(result, product.id);
      }
    });
  }

  saveProduct(data: any, productId: string | null) {
    const formData = new FormData();
    formData.append('Name', data.name);
    formData.append('CategoryId', data.categoryId);
    formData.append('Price', data.price.toString());
    formData.append('Stock', data.stock.toString());
    formData.append('DiscountPercent', data.discountPercent?.toString() || '0');
    formData.append('Description', data.description || '');

    if (data.imageFile) formData.append('ImageFile', data.imageFile);
    if (data.isImageRemoved) formData.append('DeleteCurrentImage', 'true');
    if (data.specificationsJson) formData.append('Specifications', data.specificationsJson);

    if (data.discountPercent > 0) {
      if (data.discountEndDate) formData.append('DiscountEndDate', data.discountEndDate);
      formData.append('SaleStock', data.saleStock?.toString() || '0');
    }

    if (productId) {
      // UPDATE
      this.productService.updateProduct(productId, formData).subscribe({
        next: () => {
          Swal.fire('Updated!', 'Product updated successfully.', 'success');
          this.loadProducts();
        },
        error: (err) => Swal.fire('Failed', err.error?.message, 'error')
      });
    } else {
      // CREATE
      this.productService.createProduct(formData).subscribe({
        next: () => {
          Swal.fire('Created!', 'Product created successfully.', 'success');
          this.loadProducts();
        },
        error: (err) => Swal.fire('Failed', err.error?.message, 'error')
      });
    }
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
