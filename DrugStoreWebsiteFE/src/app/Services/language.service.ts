import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { BehaviorSubject } from 'rxjs';

export type SupportedLang = 'en' | 'vi';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly STORAGE_KEY = 'app_language';
  private readonly DEFAULT_LANG: SupportedLang = 'en';

  private _currentLang = new BehaviorSubject<SupportedLang>(this.DEFAULT_LANG);
  currentLang$ = this._currentLang.asObservable();

  get currentLang(): SupportedLang {
    return this._currentLang.value;
  }

  constructor(private translate: TranslateService) {
    this.init();
  }

  private init(): void {
    this.translate.addLangs(['en', 'vi']);
    this.translate.setDefaultLang(this.DEFAULT_LANG);

    const saved = localStorage.getItem(this.STORAGE_KEY) as SupportedLang;
    const lang: SupportedLang = saved === 'vi' ? 'vi' : 'en';
    this.applyLang(lang);
  }

  setLanguage(lang: SupportedLang): void {
    this.applyLang(lang);
    localStorage.setItem(this.STORAGE_KEY, lang);
  }

  toggleLanguage(): void {
    const next: SupportedLang = this.currentLang === 'en' ? 'vi' : 'en';
    this.setLanguage(next);
  }

  private applyLang(lang: SupportedLang): void {
    this.translate.use(lang);
    this._currentLang.next(lang);
    document.documentElement.lang = lang;
  }
}
