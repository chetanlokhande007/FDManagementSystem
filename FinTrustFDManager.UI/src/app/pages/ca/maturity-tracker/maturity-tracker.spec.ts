import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MaturityTracker } from './maturity-tracker';

describe('MaturityTracker', () => {
  let component: MaturityTracker;
  let fixture: ComponentFixture<MaturityTracker>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MaturityTracker],
    }).compileComponents();

    fixture = TestBed.createComponent(MaturityTracker);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
