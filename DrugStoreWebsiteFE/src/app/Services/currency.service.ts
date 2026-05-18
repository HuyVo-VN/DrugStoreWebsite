import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, catchError, of } from 'rxjs';

export type SupportedCurrency = 'USD' | 'VND';

@Injectable({ providedIn: 'root' })
export class CurrencyService {
  // Tỷ giá dự phòng nếu API thất bại
  private readonly FALLBACK_RATE = 26000;
  private readonly STORAGE_KEY = 'app_currency';
  private readonly API_URL =
    'https://api.exchangerate-api.com/v4/latest/USD';

  private _exchangeRate = new BehaviorSubject<number>(this.FALLBACK_RATE);
  exchangeRate$ = this._exchangeRate.asObservable();

  private _currency = new BehaviorSubject<SupportedCurrency>('USD');
  currency$ = this._currency.asObservable();

  get currentCurrency(): SupportedCurrency {
    return this._currency.value;
  }

  get currentRate(): number {
    return this._exchangeRate.value;
  }

  constructor(private http: HttpClient) {
    this.init();
  }

  private init(): void {
    // Khôi phục lựa chọn tiền tệ đã lưu
    const saved = localStorage.getItem(this.STORAGE_KEY) as SupportedCurrency;
    if (saved === 'VND') {
      this._currency.next('VND');
    }
    // Lấy tỷ giá thực tế
    this.fetchExchangeRate();
  }

  fetchExchangeRate(): void {
    this.http
      .get<{ rates: Record<string, number> }>(this.API_URL)
      .pipe(
        catchError(() => {
          console.warn('CurrencyService: API failed, using fallback rate.');
          return of(null);
        })
      )
      .subscribe((res) => {
        if (res?.rates?.['VND']) {
          this._exchangeRate.next(res.rates['VND']);
        }
      });
  }

  setCurrency(currency: SupportedCurrency): void {
    this._currency.next(currency);
    localStorage.setItem(this.STORAGE_KEY, currency);
  }

  toggleCurrency(): void {
    const next: SupportedCurrency = this.currentCurrency === 'USD' ? 'VND' : 'USD';
    this.setCurrency(next);
  }

  /**
   * Convert giá từ USD sang đơn vị tiền đang chọn.
   * @param priceInUsd Giá gốc từ DB (USD)
   */
  convert(priceInUsd: number): number {
    if (this.currentCurrency === 'VND') {
      return priceInUsd * this.currentRate;
    }
    return priceInUsd;
  }

  /**
   * Format số tiền thành chuỗi có ký hiệu đơn vị.
   * @param priceInUsd Giá gốc từ DB (USD)
   */
  format(priceInUsd: number): string {
    const converted = this.convert(priceInUsd);
    if (this.currentCurrency === 'VND') {
      return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        maximumFractionDigits: 0,
      }).format(converted);
    }
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(converted);
  }
}
