import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApprovalDetails } from './approval-details';

describe('ApprovalDetails', () => {
  let component: ApprovalDetails;
  let fixture: ComponentFixture<ApprovalDetails>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApprovalDetails],
    }).compileComponents();

    fixture = TestBed.createComponent(ApprovalDetails);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
