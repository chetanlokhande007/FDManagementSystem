import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DayCountConventions } from './day-count-conventions';

describe('DayCountConventions', () => {
  let component: DayCountConventions;
  let fixture: ComponentFixture<DayCountConventions>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DayCountConventions],
    }).compileComponents();

    fixture = TestBed.createComponent(DayCountConventions);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
