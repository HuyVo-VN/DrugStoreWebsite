import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LangCurrencySwitcher } from './lang-currency-switcher';

describe('LangCurrencySwitcher', () => {
  let component: LangCurrencySwitcher;
  let fixture: ComponentFixture<LangCurrencySwitcher>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LangCurrencySwitcher]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LangCurrencySwitcher);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
