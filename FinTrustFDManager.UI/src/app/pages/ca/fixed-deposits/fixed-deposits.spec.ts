import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FixedDeposits } from './fixed-deposits';

describe('FixedDeposits', () => {
  let component: FixedDeposits;
  let fixture: ComponentFixture<FixedDeposits>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FixedDeposits],
    }).compileComponents();

    fixture = TestBed.createComponent(FixedDeposits);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
