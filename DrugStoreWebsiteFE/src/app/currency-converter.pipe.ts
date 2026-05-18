import { Pipe, PipeTransform, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { CurrencyService, SupportedCurrency } from './Services/currency.service';

/**
 * Dùng trong template để convert giá USD từ DB sang đơn vị tiền hiện tại.
 *
 * Ví dụ:
 *   {{ product.price | currencyConvert }}
 *   Kết quả: "$12.50" (USD) hoặc "325,000 ₫" (VNĐ)
 */
@Pipe({
  name: 'currencyConvert',
  standalone: true,
  pure: false, // impure để re-render khi rate/currency thay đổi
})
export class CurrencyConverterPipe implements PipeTransform, OnDestroy {
  private rate: number;
  private currency: string;
  private sub: Subscription;

  constructor(private currencyService: CurrencyService) {
    this.rate = currencyService.currentRate;
    this.currency = currencyService.currentCurrency;

    this.sub = currencyService.currency$.subscribe((c) => {
      this.currency = c;
    });
  }

  transform(priceInUsd: number | null | undefined): string {
    if (priceInUsd == null) return '';
    return this.currencyService.format(priceInUsd);
  }

  ngOnDestroy(): void {
    if (this.sub) {
      this.sub.unsubscribe();
    }
  }
}
