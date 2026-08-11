import { TestBed } from '@angular/core/testing';

import { Maturity } from './maturity';

describe('Maturity', () => {
  let service: Maturity;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Maturity);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
