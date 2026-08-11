import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateFd } from './create-fd';

describe('CreateFd', () => {
  let component: CreateFd;
  let fixture: ComponentFixture<CreateFd>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateFd],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateFd);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
