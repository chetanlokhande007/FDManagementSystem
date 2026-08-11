import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Entities } from './entities';

describe('Entities', () => {
  let component: Entities;
  let fixture: ComponentFixture<Entities>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Entities],
    }).compileComponents();

    fixture = TestBed.createComponent(Entities);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
