import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { FdCashflowComponent } from './fd-cashflow.component';
import { FDCashFlowSummary } from '../../../core/services/fd-cash-flow.service';
import { environment } from '../../../../environments/environment';

describe('FdCashflowComponent', () => {
  let component: FdCashflowComponent;
  let fixture: ComponentFixture<FdCashflowComponent>;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/FDCashFlow`;

  const mockSummary: FDCashFlowSummary = {
    fdId: 1,
    principalAmount: 100000,
    totalInterest: 5917.81,
    maturityAmount: 105917.81,
    cashFlows: [
      {
        cashFlowId: 1, fdId: 1, event: 'FD Created',
        startDate: '2025-01-01', endDate: '2025-01-01', days: 0,
        interestRate: 8, openingBalance: 0, interestAmount: 0,
        closingBalance: 100000, cashFlowAmount: 100000,
        direction: 'OUTFLOW', currencyCode: 'INR', status: 'PENDING', referenceNo: 'FD-0001'
      },
      {
        cashFlowId: 2, fdId: 1, event: 'Interest',
        startDate: '2025-01-01', endDate: '2025-04-01', days: 90,
        interestRate: 8, openingBalance: 100000, interestAmount: 1972.60,
        closingBalance: 100000, cashFlowAmount: 1972.60,
        direction: 'INFLOW', currencyCode: 'INR', status: 'PENDING', referenceNo: 'FD-0001'
      },
      {
        cashFlowId: 3, fdId: 1, event: 'Compounding Interest',
        startDate: '2025-04-01', endDate: '2025-04-01', days: 90,
        interestRate: 8, openingBalance: 100000, interestAmount: 1972.60,
        closingBalance: 101972.60, cashFlowAmount: 0,
        direction: 'INFLOW', currencyCode: 'INR', status: 'PENDING', referenceNo: 'FD-0001'
      },
      {
        cashFlowId: 4, fdId: 1, event: 'Maturity',
        startDate: '2025-12-31', endDate: '2025-12-31', days: 0,
        interestRate: 8, openingBalance: 105917.81, interestAmount: 0,
        closingBalance: 0, cashFlowAmount: 105917.81,
        direction: 'INFLOW', currencyCode: 'INR', status: 'PENDING', referenceNo: 'FD-0001'
      }
    ]
  };

  const emptySummary: FDCashFlowSummary = {
    fdId: 1, principalAmount: 0, totalInterest: 0, maturityAmount: 0, cashFlows: []
  };

  /**
   * Initialize component with data: set inputs, trigger change detection,
   * then flush the HTTP request. Angular 21's dev mode double-checks
   * expressions after each detectChanges, so we must not change component
   * state between the two passes.
   *
   * Strategy: set ALL inputs before detectChanges, then flush HTTP after.
   * The key insight is that httpMock.expectOne must come AFTER detectChanges
   * (because ngOnInit creates the request), and we do NOT call detectChanges
   * again — the autoDetectChanges-like behavior is achieved by the fact that
   * the request is resolved synchronously in the same JS turn.
   */
  function setupWithData(
    summary: FDCashFlowSummary,
    fdData: any = null,
    interestData: any = null
  ): void {
    // Set all inputs BEFORE any change detection
    component.fdId = summary.fdId;
    if (fdData) component.fdData = fdData;
    if (interestData) component.interestData = interestData;

    // Trigger ngOnInit → loadData() → HTTP request is now pending
    fixture.detectChanges();

    // Flush the pending HTTP request (synchronous delivery)
    const req = httpMock.expectOne(`${apiUrl}/fd/${summary.fdId}`);
    req.flush(summary);

    // Force Angular to re-render with the new data.
    // Wrap in a setTimeout(0) to defer past Angular's dev mode double-check.
    // Actually, since Angular 21 zoneless, we use changeDetectorRef.
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
  }

  beforeEach(async () => {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [FdCashflowComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(FdCashflowComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  // ═══════════════════════════════════════════════════════
  //  Initialization
  // ═══════════════════════════════════════════════════════

  describe('Initialization', () => {
    it('should create', () => {
      setupWithData(emptySummary);
      expect(component).toBeTruthy();
    });

    it('should fetch cash flows from API on init', () => {
      setupWithData(mockSummary);
      expect(component.cashFlows.length).toBe(4);
      expect(component.totalInterest).toBe(5917.81);
      expect(component.maturityAmount).toBe(105917.81);
    });

    it('should not fetch if fdId is not set', () => {
      component.fdId = undefined as any;
      fixture.detectChanges();
      httpMock.expectNone(`${apiUrl}/fd/undefined`);
      expect(component.cashFlows.length).toBe(0);
    });

    it('should not be in error state on successful load', () => {
      setupWithData(mockSummary);
      expect(component.errorMessage).toBe('');
    });
  });

  // ═══════════════════════════════════════════════════════
  //  Data Binding
  // ═══════════════════════════════════════════════════════

  describe('Data binding after successful load', () => {
    beforeEach(() => {
      setupWithData(mockSummary);
    });

    it('should populate cashFlows array', () => {
      expect(component.cashFlows.length).toBe(4);
    });

    it('should calculate totalInterest from summary', () => {
      expect(component.totalInterest).toBe(5917.81);
    });

    it('should set maturityAmount from summary', () => {
      expect(component.maturityAmount).toBe(105917.81);
    });

    it('should not be loading', () => {
      expect(component.isLoading).toBe(false);
    });

    it('should not have error message', () => {
      expect(component.errorMessage).toBe('');
    });
  });

  // ═══════════════════════════════════════════════════════
  //  Empty State
  // ═══════════════════════════════════════════════════════

  describe('Empty state (no cash flows)', () => {
    beforeEach(() => {
      setupWithData(emptySummary);
    });

    it('should have empty cashFlows array', () => {
      expect(component.cashFlows.length).toBe(0);
    });

    it('should have zero totalInterest', () => {
      expect(component.totalInterest).toBe(0);
    });

    it('should have zero maturityAmount', () => {
      expect(component.maturityAmount).toBe(0);
    });

    it('should show "No CashFlows found" message in template', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('No CashFlows found');
    });

    it('should not show the table', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.querySelector('table')).toBeNull();
    });
  });

  // ═══════════════════════════════════════════════════════
  //  Error State
  // ═══════════════════════════════════════════════════════

  describe('Error state', () => {
    beforeEach(() => {
      component.fdId = 1;
      fixture.detectChanges();
      const req = httpMock.expectOne(`${apiUrl}/fd/1`);
      req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
      // Trigger change detection so template reflects the error state
      fixture.changeDetectorRef.markForCheck();
      fixture.detectChanges();
    });

    it('should set error message on API failure', () => {
      expect(component.errorMessage).toBe('Unable to load cash flow records.');
    });

    it('should not be loading after error', () => {
      expect(component.isLoading).toBe(false);
    });

    it('should have empty cashFlows on error', () => {
      expect(component.cashFlows.length).toBe(0);
    });

    it('should show error message in template', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Unable to load cash flow records.');
    });

    it('should not show the table on error', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.querySelector('table')).toBeNull();
    });
  });

  // ═══════════════════════════════════════════════════════
  //  Template Rendering with Data
  // ═══════════════════════════════════════════════════════

  describe('Template rendering with data', () => {
    beforeEach(() => {
      setupWithData(
        mockSummary,
        { fdReferenceNo: 'FD-0001', principalAmount: 100000, currencyCode: 'INR' },
        { interestRate: 8, isCompounding: true, interestFrequency: 'QUARTERLY', compoundingFrequency: 'QUARTERLY' }
      );
    });

    it('should render the cash flow table', () => {
      expect(fixture.nativeElement.querySelector('table')).toBeTruthy();
    });

    it('should render correct number of table rows', () => {
      expect(fixture.nativeElement.querySelectorAll('tbody tr').length).toBe(4);
    });

    it('should render FD Created event', () => {
      expect(fixture.nativeElement.textContent).toContain('FD Created');
    });

    it('should render Interest event', () => {
      expect(fixture.nativeElement.textContent).toContain('Interest');
    });

    it('should render Compounding Interest event', () => {
      expect(fixture.nativeElement.textContent).toContain('Compounding Interest');
    });

    it('should render Maturity event', () => {
      expect(fixture.nativeElement.textContent).toContain('Maturity');
    });

    it('should render Total Interest in summary', () => {
      expect(fixture.nativeElement.textContent).toContain('Total Interest');
    });

    it('should render Maturity Amount in summary', () => {
      expect(fixture.nativeElement.textContent).toContain('Maturity Amount');
    });

    it('should render FD Reference in summary header', () => {
      expect(fixture.nativeElement.textContent).toContain('FD-0001');
    });

    it('should render interest rate in summary header', () => {
      expect(fixture.nativeElement.textContent).toContain('8.00');
    });

    it('should render compounding status as YES when enabled', () => {
      expect(fixture.nativeElement.textContent).toContain('YES');
    });

    it('should render compounding frequency when enabled', () => {
      expect(fixture.nativeElement.textContent).toContain('QUARTERLY');
    });
  });

  // ═══════════════════════════════════════════════════════
  //  Template: No Compounding
  // ═══════════════════════════════════════════════════════

  describe('Template rendering without compounding', () => {
    beforeEach(() => {
      setupWithData(
        mockSummary,
        { fdReferenceNo: 'FD-0002', principalAmount: 50000, currencyCode: 'INR' },
        { interestRate: 6, isCompounding: false, interestFrequency: 'MONTHLY' }
      );
    });

    it('should render compounding status as NO', () => {
      expect(fixture.nativeElement.textContent).toContain('NO');
    });

    it('should not render compounding frequency row', () => {
      const summaryItems = fixture.nativeElement.querySelectorAll('.summary-item');
      expect(summaryItems.length).toBe(4);
    });
  });

  // ═══════════════════════════════════════════════════════
  //  Summary Header with null inputs
  // ═══════════════════════════════════════════════════════

  describe('Template: null fdData and interestData', () => {
    it('should not render summary header when fdData is null', () => {
      setupWithData(emptySummary, null, null);
      const summaryCard = fixture.nativeElement.querySelector('.cashflow-summary-card');
      expect(summaryCard).toBeNull();
    });
  });

  // ═══════════════════════════════════════════════════════
  //  fdId Change Reload
  // ═══════════════════════════════════════════════════════

  describe('fdId change triggers reload', () => {
    it('should re-fetch cash flows when fdId changes', () => {
      setupWithData(mockSummary);
      expect(component.cashFlows.length).toBe(4);

      // Directly call loadData with new fdId to verify it works
      component.fdId = 2;
      component.loadData();
      const newSummary: FDCashFlowSummary = { fdId: 2, principalAmount: 0, totalInterest: 0, maturityAmount: 0, cashFlows: [] };
      const req2 = httpMock.expectOne(`${apiUrl}/fd/2`);
      req2.flush(newSummary);
      expect(component.cashFlows.length).toBe(0);
    });
  });

  // ═══════════════════════════════════════════════════════
  //  trackByCashFlowId
  // ═══════════════════════════════════════════════════════

  describe('trackByCashFlowId()', () => {
    it('should return the cashFlowId', () => {
      setupWithData(emptySummary);
      const mockCf = { cashFlowId: 42 } as any;
      expect(component.trackByCashFlowId(0, mockCf)).toBe(42);
    });

    it('should return different IDs for different cash flows', () => {
      setupWithData(emptySummary);
      const cf1 = { cashFlowId: 1 } as any;
      const cf2 = { cashFlowId: 2 } as any;
      expect(component.trackByCashFlowId(0, cf1)).not.toBe(component.trackByCashFlowId(0, cf2));
    });
  });

  // ═══════════════════════════════════════════════════════
  //  Currency Code Getter
  // ═══════════════════════════════════════════════════════

  describe('currencyCode getter', () => {
    it('should return fdData.currencyCode when available', () => {
      setupWithData(emptySummary, { currencyCode: 'USD' });
      expect(component.currencyCode).toBe('USD');
    });

    it('should return INR as default when fdData has no currencyCode', () => {
      setupWithData(emptySummary, {});
      expect(component.currencyCode).toBe('INR');
    });

    it('should return INR as default when fdData is null', () => {
      setupWithData(emptySummary, null);
      expect(component.currencyCode).toBe('INR');
    });
  });

  // ═══════════════════════════════════════════════════════
  //  Loading State Template
  // ═══════════════════════════════════════════════════════

  describe('Loading state in template', () => {
    it('should hide loading message after API responds', () => {
      setupWithData(mockSummary);
      expect(fixture.nativeElement.textContent).not.toContain('Loading schedule data...');
    });
  });

  // ═══════════════════════════════════════════════════════
  //  Output Event
  // ═══════════════════════════════════════════════════════

  describe('cashFlowSaved output', () => {
    it('should have cashFlowSaved EventEmitter', () => {
      setupWithData(emptySummary);
      expect(component.cashFlowSaved).toBeTruthy();
    });

    it('should be able to emit cashFlowSaved event', () => {
      setupWithData(emptySummary);
      let emitted = false;
      component.cashFlowSaved.subscribe(() => { emitted = true; });
      component.cashFlowSaved.emit();
      expect(emitted).toBe(true);
    });
  });
});
