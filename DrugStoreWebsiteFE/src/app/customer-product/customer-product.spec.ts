import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomerProduct } from './customer-product';

describe('CustomerProduct', () => {
  let component: CustomerProduct;
  let fixture: ComponentFixture<CustomerProduct>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerProduct]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CustomerProduct);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
