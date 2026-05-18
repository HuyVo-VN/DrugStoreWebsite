import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LanguageService, SupportedLang } from '../Services/language.service';
import { CurrencyService, SupportedCurrency } from '../Services/currency.service';

@Component({
  selector: 'app-lang-currency-switcher',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './lang-currency-switcher.html',
  styleUrl: './lang-currency-switcher.css',
})
export class LangCurrencySwitcher implements OnInit {
  currentLang: SupportedLang = 'en';
  currentCurrency: SupportedCurrency = 'USD';

  constructor(
    private languageService: LanguageService,
    private currencyService: CurrencyService
  ) { }

  ngOnInit(): void {
    this.languageService.currentLang$.subscribe((lang) => {
      this.currentLang = lang;
    });
    this.currencyService.currency$.subscribe((currency) => {
      this.currentCurrency = currency;
    });
  }

  toggleLang(): void {
    this.languageService.toggleLanguage();
  }

  toggleCurrency(): void {
    this.currencyService.toggleCurrency();
  }
}
