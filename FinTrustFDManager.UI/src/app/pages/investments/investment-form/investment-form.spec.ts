import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InvestmentForm } from './investment-form';

describe('InvestmentForm', () => {
  let component: InvestmentForm;
  let fixture: ComponentFixture<InvestmentForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvestmentForm],
    }).compileComponents();

    fixture = TestBed.createComponent(InvestmentForm);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
