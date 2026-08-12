import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CounterpartiesComponent } from './counterparties.component';

describe('CounterpartiesComponent', () => {
  let component: CounterpartiesComponent;
  let fixture: ComponentFixture<CounterpartiesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CounterpartiesComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CounterpartiesComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
