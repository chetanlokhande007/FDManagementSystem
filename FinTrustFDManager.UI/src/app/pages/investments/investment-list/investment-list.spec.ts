import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InvestmentList } from './investment-list';

describe('InvestmentList', () => {
  let component: InvestmentList;
  let fixture: ComponentFixture<InvestmentList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvestmentList],
    }).compileComponents();

    fixture = TestBed.createComponent(InvestmentList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
