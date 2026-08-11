import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Counterparties } from './counterparties';

describe('Counterparties', () => {
  let component: Counterparties;
  let fixture: ComponentFixture<Counterparties>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Counterparties],
    }).compileComponents();

    fixture = TestBed.createComponent(Counterparties);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
