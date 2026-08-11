import { TestBed } from '@angular/core/testing';

import { Investment } from './investment';

describe('Investment', () => {
  let service: Investment;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Investment);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
