import { Component, OnInit } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FdCashflowComponent } from '../fd-cashflow/fd-cashflow.component';
import { FdInterestComponent } from '../fd-interest/fd-interest.component';

import {
  EntityService,
  EntityDto
} from '../../../core/services/entity.service';

import {
  CounterPartyService,
  CounterParty
} from '../../../core/services/counter-party.service';

import {
  CurrencyService,
  Currency
} from '../../../core/services/currency.service';

import {
  CountryService,
  Country
} from '../../../core/services/country.service';

import {
  InterestFrequencyService,
  InterestFrequency
} from '../../../core/services/interest-frequency.service';

import {
  DayCountConventionService,
  DayCountConvention
} from '../../../core/services/day-count-convention.service';

import {
  FDIdentificationService
} from '../../../core/services/fd-identification.service';

import {
  FDCashFlowService,
  FDCashFlow
} from '../../../core/services/fd-cash-flow.service';

import {
  FDInterestService
} from '../../../core/services/fd-interest.service';

import { forkJoin, of, Observable } from 'rxjs';
import { catchError, map, switchMap, finalize, tap } from 'rxjs/operators';

let cachedCoreData: any = null;
const CORE_DATA_CACHE_KEY = 'FINTRUST_CORE_DATA';

@Component({
  selector: 'app-fd-detail',
  standalone: true,

  imports: [
    CommonModule,
    FormsModule,
    FdCashflowComponent,
    FdInterestComponent
  ],

  templateUrl: './fd-detail.component.html',
  styleUrls: ['./fd-detail.component.css']
})
export class FDDetailComponent implements OnInit {

  activeTab = 'general';

  fdId: number | null = null;

  isEdit = false;

  isSaving = false;

  loading = false;

  interestData: any = null;

  cashFlowSummary: any = null;

  isGeneralReadOnly = false;

  /* =========================
     CORE DATA
  ========================= */

  entities: EntityDto[] = [];

  counterparties: CounterParty[] = [];

  currencies: Currency[] = [];

  countries: Country[] = [];

  interestFrequencies: InterestFrequency[] = [];

  dayCountConventions: DayCountConvention[] = [];


  /* =========================
     FD FORM MODEL
  ========================= */

  fixedDeposit: any = {

    fdReferenceNo: '',

    entityId: null,

    counterpartyId: null,

    currencyCode: '',

    principalAmount: null,

    startDate: '',

    endDate: '',

    settlementDate: '',

    remarks: ''

  };


  constructor(

    private entityService: EntityService,

    private counterPartyService: CounterPartyService,

    private currencyService: CurrencyService,

    private countryService: CountryService,

    private interestFrequencyService:
      InterestFrequencyService,

    private dayCountConventionService:
      DayCountConventionService,

    private fdService:
      FDIdentificationService,

    private fdInterestService:
      FDInterestService,

    private cashFlowService:
      FDCashFlowService,

    private route: ActivatedRoute,

    private router: Router,

    private location: Location

  ) { }


  ngOnInit(): void {
    this.route.paramMap.pipe(
      tap(params => {
        const tab = this.route.snapshot.queryParamMap.get('tab');
        if (tab === 'cashflow' || tab === 'interest' || tab === 'general') {
          this.activeTab = tab;
        } else {
          this.activeTab = 'general';
        }
      }),
      map(params => Number(params.get('id'))),
      tap(newFdId => {
        if (newFdId && this.fdId !== null && this.fdId !== newFdId) {
          sessionStorage.removeItem(`FINTRUST_FD_DETAIL_CACHE_${newFdId}`);
        }

        if (newFdId) {
          this.fdId = newFdId;
          this.isEdit = true;
          const tab = this.route.snapshot.queryParamMap.get('tab');
          this.isGeneralReadOnly = (tab === 'interest' || tab === 'cashflow');
        } else {
          this.fdId = null;
          this.isEdit = false;
          this.isGeneralReadOnly = false;
        }
      }),
      tap(() => this.loading = true),
      switchMap(id => {
        // Core data can load in the background. We only block the UI on core data if on General tab.
        if (this.activeTab !== 'general') {
          this.loadCoreData().subscribe();
          if (this.isEdit && id) {
            return this.loadFDDetails(id, false, true);
          }
          this.loading = false;
          return of(null);
        }

        // On general tab, wait for both
        const fdDetails$ = (this.isEdit && id)
          ? this.loadFDDetails(id, true, false)
          : of(null);

        return forkJoin({
          core: this.loadCoreData(),
          details: fdDetails$
        }).pipe(
          finalize(() => this.loading = false)
        );
      })
    ).subscribe({
      error: (error) => {
        console.error('Error in route pipeline:', error);
        this.loading = false;
      }
    });
  }



  /* =========================
     LOAD ALL CORE DATA
  ========================= */

  loadCoreData(): Observable<any> {
    let hasCache = false;
    if (cachedCoreData) {
      this.applyCoreData(cachedCoreData, true);
      hasCache = true;
    } else {
      const storedCache = sessionStorage.getItem(CORE_DATA_CACHE_KEY);
      if (storedCache) {
        cachedCoreData = JSON.parse(storedCache);
        this.applyCoreData(cachedCoreData, true);
        hasCache = true;
      }
    }

    if (hasCache) {
      return of(cachedCoreData);
    }

    return forkJoin({
      entities: this.entityService.getAll().pipe(catchError((err) => { console.error('Entity API Error:', err); return of([]); })),
      counterparties: this.counterPartyService.getAll().pipe(catchError((err) => { console.error('Counterparty API Error:', err); return of([]); })),
      currencies: this.currencyService.getAll().pipe(catchError((err) => { console.error('Currency API Error:', err); return of([]); })),
      countries: this.countryService.getAll().pipe(catchError((err) => { console.error('Country API Error:', err); return of([]); })),
      interestFrequencies: this.interestFrequencyService.getAll().pipe(catchError((err) => { console.error('Interest Frequency API Error:', err); return of([]); })),
      dayCountConventions: this.dayCountConventionService.getAll().pipe(catchError((err) => { console.error('Day Count API Error:', err); return of([]); }))
    }).pipe(
      tap(results => {
        cachedCoreData = results;
        sessionStorage.setItem(CORE_DATA_CACHE_KEY, JSON.stringify(results));
        this.applyCoreData(results, false);
      }),
      finalize(() => {
        // Core data loading is complete. 
      })
    );
  }

  private applyCoreData(cache: any, isBackground: boolean = false): void {
    this.entities = cache.entities.filter((x: any) => x.status === 1);
    this.counterparties = cache.counterparties.filter((x: any) => x.isActive);
    this.currencies = cache.currencies.filter((x: any) => x.isActive);
    this.countries = cache.countries.filter((x: any) => x.isActive);
    this.interestFrequencies = cache.interestFrequencies;
    this.dayCountConventions = cache.dayCountConventions;
  }


  /* =========================
     LOAD FD DETAILS
  ========================= */

  loadFDDetails(id: number, forceRefresh: boolean = false, hideUI: boolean = true): Observable<any> {
    const cacheKey = `FINTRUST_FD_DETAIL_CACHE_${id}`;
    if (forceRefresh) {
      sessionStorage.removeItem(cacheKey);
    }
    const cachedData = sessionStorage.getItem(cacheKey);

    if (cachedData && !forceRefresh) {
      const result = JSON.parse(cachedData);
      this.applyFDDetails(result);
      if (hideUI) {
        this.loading = false;
      }
      // Even when using cache, always fetch fresh cash flows in the background
      // to ensure data is up-to-date (cash flows may have been regenerated by backend).
      this.cashFlowService.getByFdId(id).pipe(
        map(res => res || null),
        catchError(() => of(null))
      ).subscribe(freshCashFlows => {
        if (freshCashFlows && freshCashFlows.schedule) {
          freshCashFlows.schedule = freshCashFlows.schedule.sort((a: any, b: any) => {
            const timeDiff = new Date(a.startDate).getTime() - new Date(b.startDate).getTime();
            return timeDiff === 0 ? a.cashFlowId - b.cashFlowId : timeDiff;
          });
        }
        this.cashFlowSummary = freshCashFlows;
        // Update cache with fresh cash flow data
        const updatedCache = sessionStorage.getItem(cacheKey);
        if (updatedCache) {
          const cached = JSON.parse(updatedCache);
          cached.cashFlowSummary = this.cashFlowSummary;
          sessionStorage.setItem(cacheKey, JSON.stringify(cached));
        }
      });
      return of(result);
    }

    if (hideUI) {
      this.loading = true;
    }

    return forkJoin({
      general: this.fdService.getById(id),
      interest: this.fdInterestService.getByFdId(id).pipe(
        map(res => res || null),
        catchError((err) => {
          if (err.status === 404 || err.status === 204) return of(null);
          throw err;
        })
      ),
      cashFlowSummary: this.cashFlowService.getByFdId(id).pipe(
        map(res => res || null),
        catchError((err) => {
          if (err.status === 404 || err.status === 204) return of(null);
          throw err;
        })
      )
    }).pipe(
      tap(result => {
        sessionStorage.setItem(cacheKey, JSON.stringify(result));
        this.applyFDDetails(result);
      }),
      catchError(error => {
        console.error('Error loading FD details:', error);
        alert('Unable to load FD details.');
        return of(null);
      }),
      finalize(() => {
        this.loading = false;
      })
    );
  }

  private applyFDDetails(result: any): void {
    if (!result) return;

    const rawFd = result.general as any;
    const fd = Array.isArray(rawFd) ? rawFd[0] : rawFd;

    this.fixedDeposit = {
      fdReferenceNo: fd?.fdReferenceNo || fd?.FdReferenceNo || '',
      entityId: fd?.entityId || fd?.EntityId || null,
      counterpartyId: fd?.counterpartyId || fd?.CounterpartyId || null,
      currencyCode: fd?.currencyCode || fd?.CurrencyCode || '',
      principalAmount: fd?.principalAmount || fd?.PrincipalAmount || null,
      startDate: this.formatDate(fd?.startDate || fd?.StartDate),
      endDate: this.formatDate(fd?.endDate || fd?.EndDate),
      settlementDate: this.formatDate(fd?.settlementDate || fd?.SettlementDate),
      remarks: fd?.remarks || fd?.Remarks || '',
      status: fd?.status || fd?.Status || ''
    };

    this.interestData = result.interest;
    if (result.cashFlowSummary && result.cashFlowSummary.schedule && Array.isArray(result.cashFlowSummary.schedule)) {
      result.cashFlowSummary.schedule = result.cashFlowSummary.schedule.sort((a: any, b: any) => {
        const timeDiff = new Date(a.startDate).getTime() - new Date(b.startDate).getTime();
        return timeDiff === 0 ? a.cashFlowId - b.cashFlowId : timeDiff;
      });
      this.cashFlowSummary = result.cashFlowSummary;
    } else {
      this.cashFlowSummary = null;
    }

  }


  /* =========================
     DATE FORMAT
  ========================= */

  formatDate(value: string | null | undefined): string {
    return value ? value.substring(0, 10) : '';
  }


  /* =========================
     TAB
  ========================= */

  selectTab(tab: string): void {
    if (tab === 'interest' || tab === 'cashflow') {
      if (!this.fdId) {
        alert('Please save the General details first.');
        return;
      }
      if (!this.isGeneralReadOnly) {
        alert('Please save or cancel your changes before switching tabs.');
        return;
      }
    }
    this.activeTab = tab;
  }

  editGeneral(): void {
    this.isGeneralReadOnly = false;
  }

  getEntityName(id: number | null): string {
    if (!id) return '';
    const e = this.entities.find(x => x.entityId === id);
    return e ? e.entityName : '';
  }

  getCounterpartyName(id: number | null): string {
    if (!id) return '';
    const cp = this.counterparties.find(x => x.counterPartyId === id);
    return cp ? cp.counterPartyName : '';
  }

  errors: any = {};

  /* =========================
     VALIDATION
  ========================= */

  validateField(field: string): void {
    if (field === 'entityId') {
      if (!this.fixedDeposit.entityId) this.errors.entityId = 'Entity is required.';
      else delete this.errors.entityId;
    }
    else if (field === 'counterpartyId') {
      if (!this.fixedDeposit.counterpartyId) this.errors.counterpartyId = 'Counterparty is required.';
      else delete this.errors.counterpartyId;
    }
    else if (field === 'currencyCode') {
      if (!this.fixedDeposit.currencyCode) this.errors.currencyCode = 'Transaction Currency is required.';
      else delete this.errors.currencyCode;
    }
    else if (field === 'principalAmount') {
      if (!this.fixedDeposit.principalAmount) {
        this.errors.principalAmount = 'Principal Amount is required.';
      } else if (Number(this.fixedDeposit.principalAmount) <= 0) {
        this.errors.principalAmount = 'Principal Amount must be greater than 0.';
      } else {
        delete this.errors.principalAmount;
      }
    }
  }

  validateDates(): void {
    const sDate = this.fixedDeposit.startDate;
    const eDate = this.fixedDeposit.endDate;
    const setDate = this.fixedDeposit.settlementDate;

    if (!sDate) this.errors.startDate = 'Start Date is required.';
    else delete this.errors.startDate;

    if (!eDate) {
      this.errors.endDate = 'End Date is required.';
    } else if (sDate && eDate <= sDate) {
      this.errors.endDate = 'End Date must be after Start Date.';
    } else {
      delete this.errors.endDate;
    }

    if (!setDate) {
      this.errors.settlementDate = 'Settlement Date is required.';
    } else if (eDate && setDate < eDate) {
      this.errors.settlementDate = 'Settlement Date must be on or after End Date.';
    } else {
      delete this.errors.settlementDate;
    }

    this.checkTenureWarning();
  }

  oddTenureWarning: string | null = null;

  checkTenureWarning(): void {
    this.oddTenureWarning = null;
    const sDate = this.fixedDeposit.startDate;
    const eDate = this.fixedDeposit.endDate;
    if (sDate && eDate && sDate < eDate) {
      const start = new Date(sDate);
      const end = new Date(eDate);

      const diffTime = Math.abs(end.getTime() - start.getTime());
      const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

      // Calculate the difference in months
      const monthsDiff = (end.getFullYear() - start.getFullYear()) * 12 + (end.getMonth() - start.getMonth());

      // Expected end date if it was exactly N months
      const expectedEnd = new Date(start.getFullYear(), start.getMonth() + monthsDiff, start.getDate());

      // Also check for exactly N years
      const yearsDiff = end.getFullYear() - start.getFullYear();
      const expectedEndYear = new Date(start.getFullYear() + yearsDiff, start.getMonth(), start.getDate());

      if (end.getTime() !== expectedEnd.getTime() && end.getTime() !== expectedEndYear.getTime()) {
        this.oddTenureWarning = `Tenure is ${diffDays} days — confirm this is intentional.`;
      }
    }
  }

  validateAll(): boolean {
    this.errors = {};
    this.validateField('entityId');
    this.validateField('counterpartyId');
    this.validateField('currencyCode');
    this.validateField('principalAmount');
    this.validateDates();
    return Object.keys(this.errors).length === 0;
  }

  /* =========================
     SAVE / UPDATE
  ========================= */

  save(): void {
    if (!this.validateAll() || this.isSaving) {
      console.log('Validation failed or saving in progress', this.errors);
      return; // Stop if invalid
    }

    this.isSaving = true;

    const payload = {
      fdId: this.fdId || 0,
      fdReferenceNo:
        this.fixedDeposit.fdReferenceNo,

      entityId: Number(this.fixedDeposit.entityId),
      counterpartyId: Number(this.fixedDeposit.counterpartyId),
      currencyCode: this.fixedDeposit.currencyCode,

      principalAmount:
        Number(this.fixedDeposit.principalAmount),

      startDate:
        this.fixedDeposit.startDate,

      endDate:
        this.fixedDeposit.endDate,

      settlementDate:
        this.fixedDeposit.settlementDate,

      remarks:
        this.fixedDeposit.remarks,

      status:
        this.fixedDeposit.status || 'DRAFT',

      bankAccountId:
        this.fixedDeposit.bankAccountId || null

    };


    console.log(
      'FD payload:',
      payload
    );


    if (this.isEdit && this.fdId) {

      this.fdService
        .update(this.fdId, payload)
        .subscribe({

          next: response => {
            console.log('FD updated:', response);
            this.isGeneralReadOnly = true;
            this.isSaving = false;

            // Refresh cash flow summary from the backend since FD dates may have changed
            // and the backend regenerates cash flows as part of FD update.
            this.cashFlowService.getByFdId(this.fdId!).pipe(
              map(res => res || null),
              catchError(() => of(null))
            ).subscribe(cashFlowSummary => {
              if (cashFlowSummary && cashFlowSummary.schedule) {
                cashFlowSummary.schedule = cashFlowSummary.schedule.sort((a: any, b: any) => {
                  const timeDiff = new Date(a.startDate).getTime() - new Date(b.startDate).getTime();
                  return timeDiff === 0 ? a.cashFlowId - b.cashFlowId : timeDiff;
                });
              }
              this.cashFlowSummary = cashFlowSummary;

              // Update session storage cache with fresh data
              const cacheKey = `FINTRUST_FD_DETAIL_CACHE_${this.fdId}`;
              const cachedData = sessionStorage.getItem(cacheKey);
              if (cachedData) {
                const result = JSON.parse(cachedData);
                result.general = response;
                result.cashFlowSummary = this.cashFlowSummary;
                sessionStorage.setItem(cacheKey, JSON.stringify(result));
              }

              this.activeTab = 'interest';
            });
          },

          error: error => {
            console.error('FD update error:', error);
            this.isSaving = false;
            if (error.error && error.error.errors) {
              this.errors = error.error.errors;
            } else {
              alert('Unable to update Fixed Deposit.');
            }
          }

        });

    }

    else {

      this.fdService
        .create(payload)
        .subscribe({

          next: response => {

            console.log(
              'FD created:',
              response
            );

            this.fdId =
              response.fdId;

            this.fixedDeposit.fdReferenceNo =
              response.fdReferenceNo;

            this.isEdit = true;
            this.isGeneralReadOnly = true;
            this.isSaving = false;

            /*
              After General Save,
              open Interest tab.
            */

            this.activeTab =
              'interest';

            // Strategy A: Change URL without triggering router to avoid duplicate load
            this.location.replaceState(`/fd-detail/${this.fdId}`);

          },

          error: error => {
            console.error('FD create error:', error);
            this.isSaving = false;
            if (error.error && error.error.errors) {
              this.errors = error.error.errors;
            } else {
              alert('Unable to create Fixed Deposit.');
            }
          }

        });

    }

  }


  /* =========================
     CANCEL
  ========================= */

  cancel(): void {

    this.router.navigate([
      '/fd'
    ]);

  }


  /* =========================
     CASH FLOW GENERATION (NOW HANDLED BY BACKEND)
  ========================= */

  onInterestSaved(interest: any): void {
    if (!this.fdId) return;

    // Update local interest state from the save response
    this.interestData = interest;

    this.loading = true;

    // Invalidate only the cash flow cache — the backend regenerates cash flows
    // as part of saving interest, so we need fresh data.
    // Use a targeted refresh: only re-fetch cash flows, not general/interest.
    this.cashFlowService.getByFdId(this.fdId).pipe(
      map(res => res || null),
      catchError(() => of(null))
    ).subscribe(cashFlowSummary => {
      if (cashFlowSummary && cashFlowSummary.schedule) {
        cashFlowSummary.schedule = cashFlowSummary.schedule.sort((a: any, b: any) => {
          const timeDiff = new Date(a.startDate).getTime() - new Date(b.startDate).getTime();
          return timeDiff === 0 ? a.cashFlowId - b.cashFlowId : timeDiff;
        });
      }
      this.cashFlowSummary = cashFlowSummary;

      // Update the session storage cache
      const cacheKey = `FINTRUST_FD_DETAIL_CACHE_${this.fdId}`;
      const cachedData = sessionStorage.getItem(cacheKey);
      if (cachedData) {
        const result = JSON.parse(cachedData);
        result.interest = interest;
        result.cashFlowSummary = this.cashFlowSummary;
        sessionStorage.setItem(cacheKey, JSON.stringify(result));
      }

      this.loading = false;
      // Open Cashflow tab for the same FD ONLY AFTER data is fresh
      this.activeTab = 'cashflow';
    });
  }

  /* =========================
     BACK
  ========================= */

  goBack(): void {
    this.router.navigate([
      '/fd'
    ]);
  }

}