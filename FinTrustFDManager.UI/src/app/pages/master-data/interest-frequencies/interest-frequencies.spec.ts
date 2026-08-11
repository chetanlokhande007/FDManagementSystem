import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InterestFrequencies } from './interest-frequencies';

describe('InterestFrequencies', () => {
  let component: InterestFrequencies;
  let fixture: ComponentFixture<InterestFrequencies>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InterestFrequencies],
    }).compileComponents();

    fixture = TestBed.createComponent(InterestFrequencies);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
