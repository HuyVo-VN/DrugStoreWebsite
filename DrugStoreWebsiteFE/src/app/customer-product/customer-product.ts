import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { ProductService } from '../Services/product.service';
import Swal from 'sweetalert2';
import { LoggerService } from '../Services/logger.service';
import { debounceTime, Subject } from 'rxjs';
import { AuthService } from '../Services/auth.service';
import { CategoryService } from '../Services/category.service';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatInputModule } from '@angular/material/input';
import { CartService } from '../Services/cart.service';
import { AppRoles } from '../enums/role.enums';
import { BannerService } from '../Services/banner.service';
import { CollectionService } from '../Services/collection.service';

@Component({
  selector: 'app-customer-product',
  standalone: true,
  imports: [CommonModule, RouterModule, CurrencyPipe, FormsModule, MatFormFieldModule, MatSelectModule, MatInputModule, MatProgressSpinnerModule],
  templateUrl: './customer-product.html',
  styleUrl: './customer-product.css',

})
export class CustomerProduct implements OnInit {
  products: any[] = [];
  private readonly baseUrl = 'https://localhost:5287';
  private readonly defaultImage = '/images/default-product.png';

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    private router: Router,
    private logger: LoggerService,
    private cartService: CartService,
    private authService: AuthService,
    private route: ActivatedRoute,
    private bannerService: BannerService,
    private collectionService: CollectionService

  ) { }

  currentPage: number = 1;
  pageSize: number = 15;
  totalPages: number = 0;
  totalCount: number = 0;
  pages: number[] = [];
  productId = '';

  categories: any[] = [];
  searchSubject = new Subject<string>();
  currentKeyword: string = '';
  private categoriesMap = new Map<string, string>();
  showFilter = false;

  minPrice: number | null = null;
  maxPrice: number | null = null;

  value = '';
  viewValue = '';
  selectedCategoryId = '';
  filterCategory = '';
  loading = false;

  public AppRoles = AppRoles;
  userRole = '';

  fsHours: string = '00';
  fsMinutes: string = '00';
  fsSeconds: string = '00';
  private countdownInterval: any;

  mainBanners: any[] = [];
  currentBannerIndex = 0;
  slideInterval: any;

  sideBanners: any[] = [];

  quickButtons = [
    { icon: 'fa-solid fa-pills', label: 'Thuốc kê đơn', targetUrl: '/?category=1' },
    { icon: 'fa-solid fa-kit-medical', label: 'Thực phẩm CN', targetUrl: '/?category=2' },
    { icon: 'fa-solid fa-stethoscope', label: 'Thiết bị y tế', targetUrl: '/?category=3' },
    { icon: 'fa-solid fa-baby', label: 'Mẹ & Bé', targetUrl: '/?category=4' },
    { icon: 'fa-solid fa-hand-holding-heart', label: 'Chăm sóc cá nhân', targetUrl: '/?category=5' },
    { icon: 'fa-solid fa-percent', label: 'Khuyến mãi', targetUrl: '/promotions' }
  ];

  saleProducts: any[] = [];

  getSoldPercent(saleSold: number, saleStock: number): number {
    if (!saleStock || saleStock === 0) return 0;
    const percent = Math.round((saleSold / saleStock) * 100);
    return percent > 100 ? 100 : percent;
  }

  bestSellerProducts: any[] = [];

  homepageCollections: any[] = [];

  ngOnInit() {
    this.loadData();
    this.searchSubject.pipe(debounceTime(2000)).subscribe(keyword => {
      this.performSearch(keyword);
    });

    this.authService.role$.subscribe((role) => {
      this.userRole = role;
    });

    this.loading = true;
    this.categoryService.getCategories().subscribe({
      next: (res) => {
        if (res.status === 200 && res.data) {
          this.categoriesMap.clear();
          for (const category of res.data) {
            this.categoriesMap.set(category.id, category.name);
          }
        }

        // LẮNG NGHE URL ĐỂ TÌM KIẾM
        this.route.queryParams.subscribe(params => {
          this.currentKeyword = params['search'] || '';
          this.selectedCategoryId = params['category'] || '';
          this.currentPage = 1; // Reset trang về 1 khi có tìm kiếm mới

          if (this.currentKeyword) {
            this.performSearch(this.currentKeyword);
          } else if (this.selectedCategoryId) {
            this.applyFilterForUrl(this.selectedCategoryId);
          } else {
            this.loadProducts();
          }

          if (history.state && history.state.scrollToGrid) {
            this.scrollToProductGrid();

            window.history.replaceState({}, document.title, window.location.href);
          }
        });
      },
      error: (err) => {
        this.loading = false;
      }
    });

    this.startBannerSlide();
    this.loadMarketingData();
    this.startFlashSaleTimer();
  }

  ngOnDestroy() {
    // Delete interval when leave page to escapes memory leak
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
    if (this.slideInterval) {
      clearInterval(this.slideInterval);
    }
  }

  startFlashSaleTimer() {
    this.countdownInterval = setInterval(() => {
      if (!this.saleProducts || this.saleProducts.length === 0) return;

      const now = new Date().getTime();

      const endDateString = this.saleProducts[0].discountEndDate;

      if (!endDateString) return; 

      const endTime = new Date(endDateString).getTime();
      const diff = endTime - now;

      if (diff <= 0) {
        this.fsHours = '00'; this.fsMinutes = '00'; this.fsSeconds = '00';
        return;
      }

      const h = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
      const m = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
      const s = Math.floor((diff % (1000 * 60)) / 1000);

      this.fsHours = h < 10 ? '0' + h : h.toString();
      this.fsMinutes = m < 10 ? '0' + m : m.toString();
      this.fsSeconds = s < 10 ? '0' + s : s.toString();
    }, 1000);
  }

  // --- LOGIC FOR BANNER SLIDER ---
  startBannerSlide() {
    this.pauseBannerSlide();

    this.slideInterval = setInterval(() => {
      this.nextBanner();
    }, 4000);
  }

  pauseBannerSlide() {
    if (this.slideInterval) {
      clearInterval(this.slideInterval);
      this.slideInterval = null; // Reset về null
    }
  }

  nextBanner() {
    this.currentBannerIndex = (this.currentBannerIndex + 1) % this.mainBanners.length;
  }

  prevBanner() {
    this.currentBannerIndex = (this.currentBannerIndex - 1 + this.mainBanners.length) % this.mainBanners.length;
  }

  goToBanner(index: number) {
    this.currentBannerIndex = index;
  }

  navigateToAction(url: string) {
    if (!url || url.trim() === '') {
      console.log('This banner does not have a link attached!');
      return;
    }

    if (url.startsWith('http://') || url.startsWith('https://')) {
      window.open(url, '_blank');
      return;
    }

    const cleanUrl = url.startsWith('/') ? url : `/${url}`;

    this.router.navigateByUrl(cleanUrl).catch(err => {
      console.error('Angular Router navigation error:', err);
    });
  }

  loadMarketingData() {
    // 1. Load Banner
    this.bannerService.getActiveBanners().subscribe({
      next: (res) => {
        const banners = Array.isArray(res) ? res : (res.value || res.data);

        if (banners && banners.length > 0) {

          this.mainBanners = banners.filter((b: any) => b.displayOrder < 10);

          this.sideBanners = banners
            .filter((b: any) => b.displayOrder >= 10)
            .sort((a: any, b: any) => a.displayOrder - b.displayOrder);

          if (this.mainBanners.length > 0) {
            this.startBannerSlide();
          }
        }
      },
      error: (err) => console.log('Error loading banners', err)
    });

    // 2. Load Sale Products
    this.productService.getSaleProducts(10).subscribe({
      next: (res) => {
        console.log('Data API Sale Products return:', res);

        const pagedResult = res.data || res.value;

        if (pagedResult && pagedResult.items && pagedResult.items.length > 0) {
          this.saleProducts = pagedResult.items;
        } else {
          console.log('No sale items were found in the items section.');
        }
      },
      error: (err) => console.error('Error loading sale products', err)
    });

    // 3. Load Best Sellers
    this.productService.getBestSellers(10).subscribe({
      next: (res) => {
        if (res.isSuccess && res.value && res.value.items) {
          this.bestSellerProducts = res.value.items;
        }
      },
      error: (err) => console.log('Error loading best sellers', err)
    });

    // 4. Dynamic Collections
    this.collectionService.getHomepageCollections(5).subscribe({
      next: (res) => {
        const collections = Array.isArray(res) ? res : (res.value || res.data);
        if (collections && collections.length > 0) {
          this.homepageCollections = collections;
        }
      },
      error: (err) => console.log('Error loading homepage collections', err)
    });
  }

  applyFilterForUrl(categoryId: string) {
    this.productService.filterProducts(categoryId, this.currentPage, this.pageSize).subscribe({
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
        this.loading = false;
      }
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

          this.products = pagedResult.items;

          //Pagination
          this.totalPages = pagedResult.totalPages || 0;
          this.totalCount = pagedResult.totalCount || 0;
          this.pages = Array.from({ length: this.totalPages }, (_, i) => i + 1);

          window.scrollTo({ top: 0, behavior: 'smooth' }); //scroll to top when pagination
        }
      },
      error: (err) => {
        this.logger.error('Failed to load products, Error: ${err}');
        Swal.fire('Error', 'Failed to load products', 'error');
      }
    });
  }

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
    this.scrollToProductGrid();
  }

  getProductImageUrl(imageUrl: string | null): string {
    if (!imageUrl || imageUrl === 'null') {
      return this.defaultImage;
    }
    if (imageUrl.startsWith('http')) {
      return imageUrl;
    }

    const cleanPath = imageUrl.startsWith('/') ? imageUrl : `/${imageUrl}`;
    return `${this.baseUrl}${cleanPath}`;
  }

  goToProductDetails(productId: string) {
    this.productService.setProductId(productId);
    this.router.navigate(['/product-detail']);
  }

  addToCart(event: Event, product: any) {
    event.stopPropagation();
    if (this.userRole) {
      this.cartService.addToCart(product.id, 1).subscribe({
        next: () => {
          Swal.fire({
            icon: 'success',
            title: 'Added to Cart',
            text: `${product.name} has been added to your cart!`,
            timer: 1500,
            showConfirmButton: false
          });

          this.cartService.getCart().subscribe(res => {
            const items = res.data?.items || [];
            this.cartService.setQuantity(items.length);
          });
        }
      });
    }
    else {
      Swal.fire({
        icon: 'warning',
        title: 'Login Required',
        text: 'Please log in to continue adding items to your cart!',
        showCancelButton: true,
        confirmButtonText: 'Login',
        cancelButtonText: 'Cancel',
        reverseButtons: true
      }).then((result) => {
        if (result.isConfirmed) {
          window.location.href = '/login';
        }
      });
    }
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

  toggleFilter() {
    this.showFilter = !this.showFilter;
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
            this.filterCategorySelected();
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

  reset() {
    this.minPrice = null;
    this.maxPrice = null;
    this.selectedCategoryId = '';
    this.filterCategory = '';
    this.toggleFilter();
    this.loadData();
  }

  filterCategorySelected() {
    this.filterCategory = this.categoriesMap.get(this.selectedCategoryId) || '';
  }

  resetFilterCategory() {
    this.filterCategory = '';
    this.selectedCategoryId = '';
    this.loadData();
  }

  public get isCustomer(): boolean {
    return this.userRole === this.AppRoles.Customer;
  }

  scrollToProductGrid() {
    setTimeout(() => {
      const element = document.getElementById('product-grid-section');
      if (element) {
        
        const headerOffset = 100;
        const elementPosition = element.getBoundingClientRect().top;
        const offsetPosition = elementPosition + window.scrollY - headerOffset;

        window.scrollTo({
          top: offsetPosition,
          behavior: 'smooth'
        });
      }
    }, 100);
  }

  get isSearchOrFilterActive(): boolean {
    // Trả về true nếu có từ khóa tìm kiếm HOẶC có chọn danh mục
    return (this.currentKeyword !== null && this.currentKeyword !== '') ||
      (this.selectedCategoryId !== null && this.selectedCategoryId !== '');
  }

}
