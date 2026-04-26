import { Footer } from '../footer/footer';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { Header } from '../header/header';
import { AuthService } from '../Services/auth.service';
import { Component, OnInit, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Chart, registerables } from 'chart.js';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../Services/order.service';
import { ProductService } from '../Services/product.service';
import { DashboardService } from '../Services/dashboard.service';

Chart.register(...registerables);

@Component({
  selector: 'app-admin-page',
  standalone: true,
  imports: [CommonModule, RouterModule, CurrencyPipe, FormsModule],
  templateUrl: './admin-page.html',
  styleUrl: './admin-page.css'
})
export class AdminPage implements OnInit, AfterViewInit {
  username = '';
  userRole = '';

  // Data cho các thẻ Summary phía trên
  totalRevenue: number = 0;
  totalOrders: number = 0;
  totalProducts: number = 0;

  chart: any;
  isExportingExcel = false;
  isExportingPdf = false;

  @ViewChild('revenueChart') revenueChartCanvas!: ElementRef;

  // Cấu hình Dropdown hiện tại
  selectedEntity: string = 'order';
  selectedStatType: string = 'revenue_month';
  selectedChartType: string = 'bar';

  // Biến hứng Data từ C# trả về để vẽ biểu đồ
  chartLabels: string[] = [];
  currentChartData: number[] = [];
  currentChartLabel: string = '';

  // Mảng Dropdown 1
  entities = [
    { id: 'product', name: 'Product' },
    { id: 'category', name: 'Category' },
    { id: 'order', name: 'Order' }
  ];

  // Mảng Dropdown 2
  statTypes: any = {
    product: [
      { id: 'stock', name: 'Product stock' },
      { id: 'top_selling', name: 'Top selling' }
    ],
    category: [
      { id: 'prod_per_cat', name: 'Product per category' },
      { id: 'top_cat_selling', name: 'Top category selling ($)' }
    ],
    order: [
      { id: 'revenue_month', name: 'Revenue by month' },
      { id: 'order_month', name: 'Orders by month' },
      { id: 'order_status', name: 'Order Status' }
    ]
  };

  selectedYear: string = 'all';
  selectedMonth: string = 'all';

  // Tạo mảng năm tự động (từ 2024 đến năm hiện tại + 1)
  years: number[] = [2024, 2025, 2026, 2027, 2028, 2029, 2030];
  months: number[] = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

  constructor(
    private authService: AuthService,
    private router: Router,
    private orderService: OrderService,
    private productService: ProductService,
    private dashboardService: DashboardService
  ) { }

  ngOnInit() {
    this.authService.username$.subscribe(name => this.username = name);
    this.userRole = this.authService.getUserRole();
    this.loadSummaryData(); // Load data cho 3 cục ở trên
  }

  ngAfterViewInit() {
    // Vừa vào trang là gọi API load biểu đồ mặc định luôn
    setTimeout(() => {
      this.updateChartData();
    }, 100);
  }

  // Hàm này chỉ tính toán cho 3 cái thẻ Card Summary ở trên cùng
  loadSummaryData() {
    this.orderService.getAllOrders().subscribe({
      next: (res: any) => {
        if (res.status === 200 && res.data) {
          const orders = res.data;
          this.totalOrders = orders.length;
          this.totalRevenue = 0;
          orders.forEach((order: any) => {
            const isPaid = order.status === 'Paid' || order.status === 'Completed' || order.status === 2 || order.status === 4;
            if (isPaid) this.totalRevenue += order.totalAmount;
          });
        }
      }
    });

    this.productService.getProducts(1, 1).subscribe({
      next: (res: any) => {
        if (res.status === 200 && res.data) {
          this.totalProducts = res.data.totalCount;
        }
      }
    });
  }

  get showTimeFilters(): boolean {
    const validStats = ['top_selling', 'top_cat_selling', 'revenue_month', 'order_month'];
    return validStats.includes(this.selectedStatType);
  }
  onTimeChange() {
    if (this.selectedYear === 'all') {
      this.selectedMonth = 'all';
    }
    this.updateChartData();
  }

  // --- LOGIC XỬ LÝ 3 DROPDOWN ---
  get currentStatTypes() {
    return this.statTypes[this.selectedEntity] || [];
  }

  onEntityChange() {
    // Reset option 2 về mặc định của đối tượng mới
    this.selectedStatType = this.statTypes[this.selectedEntity][0].id;
    this.updateChartData();
  }

  onStatTypeChange() {
    this.updateChartData();
  }

  onChartTypeChange() {
    this.renderChart();
  }

  // --- GỌI API & VẼ BIỂU ĐỒ ---
  updateChartData() {
    // Tự động chuyển qua biểu đồ tròn nếu xem Trạng thái hoặc Danh mục cho đẹp
    if (this.selectedStatType === 'order_status' || this.selectedStatType === 'prod_per_cat') {
      this.selectedChartType = 'pie';
    } else if (this.selectedChartType === 'pie' || this.selectedChartType === 'doughnut') {
      this.selectedChartType = 'bar'; // Đưa về cột nếu xem Doanh thu
    }

    // Gọi API sang C#
    this.dashboardService.getChartData(this.selectedEntity, this.selectedStatType, this.selectedYear, this.selectedMonth).subscribe({
      next: (res: any) => {
        if (res && res.data) {
          // Hứng dữ liệu từ DTO
          this.chartLabels = res.data.labels;
          this.currentChartData = res.data.data;
          this.currentChartLabel = res.data.chartLabel;

          // Nạp đạn xong, tiến hành vẽ
          this.renderChart();
        }
      },
      error: (err: any) => {
        console.error('Lỗi khi tải dữ liệu biểu đồ', err);
      }
    });
  }

  renderChart() {
    if (!this.revenueChartCanvas || !this.revenueChartCanvas.nativeElement) return;
    const ctx = this.revenueChartCanvas.nativeElement.getContext('2d');

    const pieColors = ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40', '#E7E9ED', '#8d94ba', '#536d94'];
    let bgColor: any = (this.selectedChartType === 'pie' || this.selectedChartType === 'doughnut') ? pieColors : 'rgba(55, 146, 179, 0.7)';
    const showScales = this.selectedChartType === 'bar' || this.selectedChartType === 'line';

    if (this.chart) {
      // Đã có biểu đồ -> Update dữ liệu để tạo hiệu ứng chuyển động
      this.chart.config.type = this.selectedChartType;
      this.chart.data.labels = this.chartLabels; // Update trục X
      this.chart.data.datasets[0].data = this.currentChartData; // Update trục Y
      this.chart.data.datasets[0].label = this.currentChartLabel; // Update Tên
      this.chart.data.datasets[0].backgroundColor = bgColor;
      this.chart.data.datasets[0].borderColor = (this.selectedChartType === 'pie' || this.selectedChartType === 'doughnut') ? '#fff' : bgColor;
      this.chart.data.datasets[0].borderRadius = this.selectedChartType === 'bar' ? 5 : 0;
      this.chart.data.datasets[0].fill = this.selectedChartType === 'line';

      if (this.chart.options.scales) {
        this.chart.options.scales = showScales ? { x: { display: true }, y: { beginAtZero: true, display: true } } : { x: { display: false }, y: { display: false } };
      }
      this.chart.update();
    } else {
      // Vẽ lần đầu tiên
      this.chart = new Chart(ctx, {
        type: this.selectedChartType as any,
        data: {
          labels: this.chartLabels,
          datasets: [{
            label: this.currentChartLabel,
            data: this.currentChartData,
            backgroundColor: bgColor,
            borderColor: (this.selectedChartType === 'pie' || this.selectedChartType === 'doughnut') ? '#fff' : bgColor,
            borderWidth: 1,
            borderRadius: this.selectedChartType === 'bar' ? 5 : 0,
            fill: this.selectedChartType === 'line',
            tension: 0.4
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          animation: { duration: 1000, easing: 'easeOutQuart' },
          scales: showScales ? { y: { beginAtZero: true } } : { x: { display: false }, y: { display: false } }
        }
      });
    }
  }

  // --- EXPORT PDF/EXCEL ---
  onExportProductsExcel() { 
    this.isExportingExcel = true;
    this.dashboardService.exportDynamicExcel(this.selectedEntity, this.selectedStatType, this.selectedYear, this.selectedMonth).subscribe({
      next: (res: any) => {
        if (res && res.downloadUrl) window.open(res.downloadUrl, '_blank');
        this.isExportingExcel = false;
      },
      error: (err: any) => {
        console.error('Error while export Excel:', err);
        Swal.fire('Error', 'An error occurred while creating the Excel report!', 'error');
        this.isExportingExcel = false;
      }
    });
  }

  onExportOrdersPdf() {
    this.isExportingPdf = true;
    this.dashboardService.exportDynamicPdf(this.selectedEntity, this.selectedStatType, this.selectedYear, this.selectedMonth).subscribe({
      next: (res: any) => {
        if (res && res.downloadUrl) window.open(res.downloadUrl, '_blank');
        this.isExportingPdf = false;
      },
      error: (err: any) => {
        console.error('Error while export PDF:', err);
        Swal.fire('Error', 'An error occurred while creating the PDF report!', 'error');
        this.isExportingPdf = false;
      }
    });
  }
}
