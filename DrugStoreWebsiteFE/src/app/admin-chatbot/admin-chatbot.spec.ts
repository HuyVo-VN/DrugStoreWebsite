import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminChatbot } from './admin-chatbot';

describe('AdminChatbot', () => {
  let component: AdminChatbot;
  let fixture: ComponentFixture<AdminChatbot>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminChatbot]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminChatbot);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
