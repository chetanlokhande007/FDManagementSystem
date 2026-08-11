import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BankAccounts } from './bank-accounts';

describe('BankAccounts', () => {
  let component: BankAccounts;
  let fixture: ComponentFixture<BankAccounts>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BankAccounts],
    }).compileComponents();

    fixture = TestBed.createComponent(BankAccounts);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
