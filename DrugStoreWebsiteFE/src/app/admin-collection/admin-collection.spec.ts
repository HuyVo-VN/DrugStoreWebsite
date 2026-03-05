import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminCollection } from './admin-collection';

describe('AdminCollection', () => {
  let component: AdminCollection;
  let fixture: ComponentFixture<AdminCollection>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminCollection]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminCollection);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
