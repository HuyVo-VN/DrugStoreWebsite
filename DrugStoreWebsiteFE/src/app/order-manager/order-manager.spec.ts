import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OrderManager } from './order-manager';

describe('OrderManager', () => {
  let component: OrderManager;
  let fixture: ComponentFixture<OrderManager>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrderManager]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OrderManager);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
