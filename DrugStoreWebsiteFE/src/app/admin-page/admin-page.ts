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


Chart.register(...registerables);

@Component({
  selector: 'app-admin-page',
  standalone: true,
  imports: [CommonModule, RouterModule, CurrencyPipe, FormsModule],
  templateUrl: './admin-page.html',
  styleUrl: './admin-page.css'
})
export class AdminPage {
  username = '';
  userRole = '';

  totalRevenue: number = 0;
  totalOrders: number = 0;
  totalProducts: number = 0;
  chart: any;

  @ViewChild('revenueChart') revenueChartCanvas!: ElementRef;

  selectedChartType: string = 'bar';
  selectedDataType: string = 'revenue';

  chartLabels = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul'];
  dataRevenue: number[] = [];
  dataOrders: number[] = [];

  constructor(
    private authService: AuthService,
    private router: Router,
    private orderService: OrderService,
    private productService: ProductService
  ) { }

  ngOnInit() {
    this.authService.username$.subscribe(name => {
      this.username = name;
    });
    this.userRole = this.authService.getUserRole();

    this.loadDashboardData();
  }

  ngAfterViewInit() {
    setTimeout(() => {
      this.renderChart();
    }, 100);
  }

  loadDashboardData() {
    this.orderService.getAllOrders().subscribe({
      next: (res: any) => {
        if (res.status === 200 && res.data) {
          const orders = res.data;
          this.totalOrders = orders.length;

          const monthlyRevenue = new Array(12).fill(0);
          const monthlyOrders = new Array(12).fill(0);

          this.totalRevenue = 0;

          orders.forEach((order: any) => {
            const isPaid = order.status === 'Paid' || order.status === 'Completed' || order.status === 2 || order.status === 4;

            if (order.orderDate) {
              const date = new Date(order.orderDate);
              const monthIndex = date.getMonth();

              monthlyOrders[monthIndex] += 1;

              if (isPaid) {
                this.totalRevenue += order.totalAmount;
                monthlyRevenue[monthIndex] += order.totalAmount;
              }
            }
          });

          this.dataRevenue = monthlyRevenue;
          this.dataOrders = monthlyOrders;

          setTimeout(() => {
            this.renderChart();
          }, 100);

          this.renderChart();
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

  onChartChange() {
    this.renderChart();
  }

  renderChart() {
    if (!this.revenueChartCanvas || !this.revenueChartCanvas.nativeElement) {
      return;
    }

    const ctx = this.revenueChartCanvas.nativeElement.getContext('2d');

    let currentData = [];
    let currentLabel = '';
    let bgColor: any = '';

    const pieColors = ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40', '#E7E9ED', '#8d94ba', '#536d94', '#2d4b73', '#162e52', '#051630'];

    if (this.selectedDataType === 'revenue') {
      currentData = this.dataRevenue;
      currentLabel = 'Revenue ($)';
      bgColor = (this.selectedChartType === 'pie' || this.selectedChartType === 'doughnut') ? pieColors : 'rgba(55, 146, 179, 0.7)';
    } else {
      currentData = this.dataOrders;
      currentLabel = 'Number of Orders';
      bgColor = (this.selectedChartType === 'pie' || this.selectedChartType === 'doughnut') ? pieColors : 'rgba(40, 167, 69, 0.7)';
    }

    const showScales = this.selectedChartType === 'bar' || this.selectedChartType === 'line';

    // =========================================================
    // CASE 1: THE CHART ALREADY EXISTS -> ONLY UPDATE TO INCLUDE ANIMATION
    // =========================================================
    if (this.chart) {
      this.chart.config.type = this.selectedChartType;

      this.chart.data.datasets[0].data = currentData;
      this.chart.data.datasets[0].label = currentLabel;
      this.chart.data.datasets[0].backgroundColor = bgColor;
      this.chart.data.datasets[0].borderColor = (this.selectedChartType === 'pie' || this.selectedChartType === 'doughnut') ? '#fff' : bgColor;
      this.chart.data.datasets[0].borderRadius = this.selectedChartType === 'bar' ? 5 : 0;
      this.chart.data.datasets[0].fill = this.selectedChartType === 'line';

      if (this.chart.options.scales) {
        this.chart.options.scales = showScales ? { x: { display: true }, y: { beginAtZero: true, display: true } } : { x: { display: false }, y: { display: false } };
      }

      this.chart.update();
    }
    // =========================================================
      // CASE 2: RUNNING FOR THE FIRST TIME -> CREATE A NEW CHART
    // =========================================================
    else {
      this.chart = new Chart(ctx, {
        type: this.selectedChartType as any,
        data: {
          labels: this.chartLabels,
          datasets: [{
            label: currentLabel,
            data: currentData,
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
          animation: {
            duration: 1000,
            easing: 'easeOutQuart'
          },
          scales: showScales ? { y: { beginAtZero: true } } : { x: { display: false }, y: { display: false } }
        }
      });
    }
  }

}
