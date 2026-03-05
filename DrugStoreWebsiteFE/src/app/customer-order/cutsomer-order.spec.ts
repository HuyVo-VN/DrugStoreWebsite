import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomerOrder } from './customer-order';

describe('CustomerOrder', () => {
  let component: CustomerOrder;
  let fixture: ComponentFixture<CustomerOrder>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerOrder]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CustomerOrder);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
