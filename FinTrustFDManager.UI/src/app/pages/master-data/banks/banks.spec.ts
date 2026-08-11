import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Banks } from './banks';

describe('Banks', () => {
  let component: Banks;
  let fixture: ComponentFixture<Banks>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Banks],
    }).compileComponents();

    fixture = TestBed.createComponent(Banks);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
