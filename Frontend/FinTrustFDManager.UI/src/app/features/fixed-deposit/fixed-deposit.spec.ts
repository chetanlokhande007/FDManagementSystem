import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FixedDeposit } from './fixed-deposit';

describe('FixedDeposit', () => {
  let component: FixedDeposit;
  let fixture: ComponentFixture<FixedDeposit>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FixedDeposit],
    }).compileComponents();

    fixture = TestBed.createComponent(FixedDeposit);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
