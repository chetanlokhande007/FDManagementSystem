import { TestBed } from '@angular/core/testing';

import { Approval } from './approval';

describe('Approval', () => {
  let service: Approval;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Approval);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
