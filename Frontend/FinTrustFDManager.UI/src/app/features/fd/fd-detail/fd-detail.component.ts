import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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

import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

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

  loading = false;

  interestData: any = null;

  cashFlows: FDCashFlow[] = [];

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

    private router: Router

  ) { }


  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');
    const tab =
      this.route.snapshot.queryParamMap.get('tab');

    if (id) {
      this.fdId = Number(id);
      this.isEdit = true;
      this.isGeneralReadOnly = true;
    }

    // Store the requested tab, but don't set activeTab yet
    // It will be set after FD details load (if navigating to interest/cashflow)
    if (tab) {
      sessionStorage.setItem('FD_REQUESTED_TAB', tab);
      // Only set directly if it's 'general'
      if (tab === 'general') {
        this.activeTab = tab;
      }
    }

    this.loadCoreData();

  }



  /* =========================
     LOAD ALL CORE DATA
  ========================= */

  loadCoreData(): void {
    let hasCache = false;
    if (cachedCoreData) {
      this.applyCoreData(cachedCoreData);
      hasCache = true;
    } else {
      const storedCache = sessionStorage.getItem(CORE_DATA_CACHE_KEY);
      if (storedCache) {
        cachedCoreData = JSON.parse(storedCache);
        this.applyCoreData(cachedCoreData);
        hasCache = true;
      }
    }

    if (!hasCache) {
      this.loading = true;
    }

    forkJoin({
      entities: this.entityService.getAll().pipe(catchError((err) => { console.error('Entity API Error:', err); return of([]); })),
      counterparties: this.counterPartyService.getAll().pipe(catchError((err) => { console.error('Counterparty API Error:', err); return of([]); })),
      currencies: this.currencyService.getAll().pipe(catchError((err) => { console.error('Currency API Error:', err); return of([]); })),
      countries: this.countryService.getAll().pipe(catchError((err) => { console.error('Country API Error:', err); return of([]); })),
      interestFrequencies: this.interestFrequencyService.getAll().pipe(catchError((err) => { console.error('Interest Frequency API Error:', err); return of([]); })),
      dayCountConventions: this.dayCountConventionService.getAll().pipe(catchError((err) => { console.error('Day Count API Error:', err); return of([]); }))
    }).subscribe({
      next: (results) => {
        cachedCoreData = results;
        sessionStorage.setItem(CORE_DATA_CACHE_KEY, JSON.stringify(results));

        this.applyCoreData(results, hasCache);
      },
      error: (error) => {
        console.error('Error loading core data', error);
        this.loading = false;
      }
    });
  }

  private applyCoreData(cache: any, isBackground: boolean = false): void {
    this.entities = cache.entities.filter((x: any) => x.status === 1);
    this.counterparties = cache.counterparties.filter((x: any) => x.isActive);
    this.currencies = cache.currencies.filter((x: any) => x.isActive);
    this.countries = cache.countries.filter((x: any) => x.isActive);
    this.interestFrequencies = cache.interestFrequencies;
    this.dayCountConventions = cache.dayCountConventions;

    if (!isBackground) {
      if (this.isEdit && this.fdId) {
        this.loadFDDetails();
      } else {
        this.loading = false;
      }
    }
  }


  /* =========================
     LOAD FD DETAILS
  ========================= */

  loadFDDetails(forceRefresh: boolean = false): void {
    if (!this.fdId) return;

    const cacheKey = `FINTRUST_FD_DETAIL_CACHE_${this.fdId}`;
    if (forceRefresh) {
      sessionStorage.removeItem(cacheKey);
    }
    const cachedData = sessionStorage.getItem(cacheKey);
    
    if (cachedData) {
      const result = JSON.parse(cachedData);
      this.applyFDDetails(result);
    } else {
      this.loading = true;
    }

    forkJoin({
      general: this.fdService.getById(this.fdId),
      interest: this.fdInterestService.getByFdId(this.fdId).pipe(
        catchError(() => of(null))
      ),
      cashFlows: this.cashFlowService.getAll().pipe(
        map(cfs => {
          const arr = Array.isArray(cfs) ? cfs : [cfs];
          return arr.filter(x => Number(x.fdId) === Number(this.fdId));
        }),
        catchError(() => of([]))
      )
    }).subscribe({
      next: (result) => {
        sessionStorage.setItem(cacheKey, JSON.stringify(result));
        this.applyFDDetails(result);
      },
      error: (error) => {
        console.error('Error loading FD details:', error);
        alert('Unable to load FD details.');
        this.loading = false;
      }
    });
  }

  private applyFDDetails(result: any): void {
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
    if (result.cashFlows && Array.isArray(result.cashFlows)) {
      this.cashFlows = result.cashFlows.sort((a: any, b: any) => new Date(a.cashFlowDate).getTime() - new Date(b.cashFlowDate).getTime());
    } else {
      this.cashFlows = result.cashFlows;
    }
    this.loading = false;

    // After FD details are loaded, set the requested tab
    const requestedTab = sessionStorage.getItem('FD_REQUESTED_TAB');
    if (requestedTab && (requestedTab === 'interest' || requestedTab === 'cashflow')) {
      this.activeTab = requestedTab;
      sessionStorage.removeItem('FD_REQUESTED_TAB');
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
    if (!this.validateAll()) {
      console.log('Validation failed', this.errors);
      return; // Stop if invalid
    }

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
            this.activeTab = 'interest';
            this.loadFDDetails();
          },

          error: error => {
            console.error('FD update error:', error);
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

            /*
              After General Save,
              open Interest tab.
            */

            this.activeTab =
              'interest';

          },

          error: error => {
            console.error('FD create error:', error);
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

    alert('Interest configuration and associated Cash Flows generated successfully.');
    this.interestData = interest;
    
    this.loadFDDetails(true); // force refresh to get new cash flows instantly
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