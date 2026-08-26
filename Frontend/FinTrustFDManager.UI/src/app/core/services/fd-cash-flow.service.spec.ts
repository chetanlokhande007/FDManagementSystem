import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { FDCashFlowService, FDCashFlow, FDCashFlowSummary } from './fd-cash-flow.service';
import { environment } from '../../../environments/environment';

describe('FDCashFlowService', () => {
  let service: FDCashFlowService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/FDCashFlow`;

  const mockCashFlow: FDCashFlow = {
    cashFlowId: 1,
    fdId: 10,
    event: 'Interest',
    startDate: '2025-01-01',
    endDate: '2025-02-01',
    days: 31,
    interestRate: 8,
    openingBalance: 100000,
    interestAmount: 680.82,
    closingBalance: 100000,
    cashFlowAmount: 680.82,
    direction: 'INFLOW',
    currencyCode: 'INR',
    status: 'PENDING',
    referenceNo: 'FD-0010'
  };

  const mockSummary: FDCashFlowSummary = {
    fdId: 10,
    principalAmount: 100000,
    totalInterest: 3254.98,
    maturityAmount: 103254.98,
    cashFlows: [mockCashFlow]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        FDCashFlowService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(FDCashFlowService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAll()', () => {
    it('should return an array of cash flows', () => {
      service.getAll().subscribe(result => {
        expect(Array.isArray(result)).toBe(true);
        expect(result.length).toBe(1);
        expect(result[0].cashFlowId).toBe(1);
      });

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('GET');
      req.flush([mockCashFlow]);
    });
  });

  describe('getById()', () => {
    it('should return a cash flow by id', () => {
      service.getById(1).subscribe(result => {
        expect(result.cashFlowId).toBe(1);
        expect(result.event).toBe('Interest');
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockCashFlow);
    });
  });

  describe('create()', () => {
    it('should POST cash flow data', () => {
      service.create(mockCashFlow).subscribe(result => {
        expect(result.cashFlowId).toBeDefined();
      });

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(mockCashFlow);
      req.flush({ ...mockCashFlow, cashFlowId: 99 });
    });
  });

  describe('update()', () => {
    it('should PUT with correct id', () => {
      service.update(1, mockCashFlow).subscribe(result => {
        expect(result).toBeTruthy();
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('PUT');
      req.flush(mockCashFlow);
    });
  });

  describe('delete()', () => {
    it('should DELETE by id', () => {
      service.delete(1).subscribe(result => {
        expect(result).toBeNull();
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('getByFdId()', () => {
    it('should return cash flow summary for an FD', () => {
      service.getByFdId(10).subscribe(result => {
        expect(result.fdId).toBe(10);
        expect(result.principalAmount).toBe(100000);
        expect(result.totalInterest).toBe(3254.98);
        expect(result.maturityAmount).toBe(103254.98);
        expect(result.cashFlows.length).toBe(1);
      });

      const req = httpMock.expectOne(`${apiUrl}/fd/10`);
      expect(req.request.method).toBe('GET');
      req.flush(mockSummary);
    });

    it('should return empty summary when no cash flows exist', () => {
      const emptySummary: FDCashFlowSummary = {
        fdId: 99,
        principalAmount: 0,
        totalInterest: 0,
        maturityAmount: 0,
        cashFlows: []
      };

      service.getByFdId(99).subscribe(result => {
        expect(result.cashFlows.length).toBe(0);
        expect(result.totalInterest).toBe(0);
      });

      const req = httpMock.expectOne(`${apiUrl}/fd/99`);
      req.flush(emptySummary);
    });

    it('should use correct URL format', () => {
      service.getByFdId(42).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/fd/42`);
      expect(req.request.url).toBe(`${apiUrl}/fd/42`);
      req.flush(mockSummary);
    });

    it('should handle compounding events in summary', () => {
      const compoundingSummary: FDCashFlowSummary = {
        fdId: 10,
        principalAmount: 60000,
        totalInterest: 2500,
        maturityAmount: 62500,
        cashFlows: [
          { ...mockCashFlow, event: 'FD Created', cashFlowAmount: 60000, direction: 'OUTFLOW' },
          { ...mockCashFlow, event: 'Compounding Interest', cashFlowAmount: 0, interestAmount: 2500 }
        ]
      };

      service.getByFdId(10).subscribe(result => {
        expect(result.cashFlows.length).toBe(2);
        const compounding = result.cashFlows.find(c => c.event === 'Compounding Interest');
        expect(compounding).toBeTruthy();
        expect(compounding!.cashFlowAmount).toBe(0);
      });

      const req = httpMock.expectOne(`${apiUrl}/fd/10`);
      req.flush(compoundingSummary);
    });
  });

  describe('Error handling', () => {
    it('should propagate 404 errors', () => {
      let errorStatus: number | undefined;
      service.getById(999).subscribe({
        next: () => { throw new Error('Expected error'); },
        error: (err) => {
          errorStatus = err.status;
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/999`);
      req.flush('Not Found', { status: 404, statusText: 'Not Found' });
      expect(errorStatus).toBe(404);
    });

    it('should propagate 500 errors', () => {
      let errorStatus: number | undefined;
      service.getAll().subscribe({
        next: () => { throw new Error('Expected error'); },
        error: (err) => {
          errorStatus = err.status;
        }
      });

      const req = httpMock.expectOne(apiUrl);
      req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
      expect(errorStatus).toBe(500);
    });

    it('should propagate 400 errors', () => {
      let errorStatus: number | undefined;
      service.create({ ...mockCashFlow, cashFlowId: 0 } as any).subscribe({
        next: () => { throw new Error('Expected error'); },
        error: (err) => {
          errorStatus = err.status;
        }
      });

      const req = httpMock.expectOne(apiUrl);
      req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });
      expect(errorStatus).toBe(400);
    });
  });
});
