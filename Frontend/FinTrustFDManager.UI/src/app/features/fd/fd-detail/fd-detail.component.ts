import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FdCashflowComponent } from '../fd-cashflow/fd-cashflow.component';
import { FdInterestComponent } from '../fd-interest/fd-interest.component';

import {
  EntityService,
  Entity
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

  entities: Entity[] = [];

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

  ) {}


  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');

    if (id) {
      this.fdId = Number(id);
      this.isEdit = true;
      this.isGeneralReadOnly = true;
    }

    this.loadCoreData();

  }


  /* =========================
     LOAD ALL CORE DATA
  ========================= */

  loadCoreData(): void {

    this.loading = true;


    this.entityService
      .getAll()
      .subscribe({

        next: data => {

          this.entities =
            data.filter(x => x.status === 1);

        },

        error: error => {

          console.error(
            'Entity API Error:',
            error
          );

        }

      });


    this.counterPartyService
      .getAll()
      .subscribe({

        next: data => {

          this.counterparties =
            data.filter(x => x.isActive);

        },

        error: error => {

          console.error(
            'Counterparty API Error:',
            error
          );

        }

      });


    this.currencyService
      .getAll()
      .subscribe({

        next: data => {

          this.currencies =
            data.filter(x => x.isActive);

        },

        error: error => {

          console.error(
            'Currency API Error:',
            error
          );

        }

      });


    this.countryService
      .getAll()
      .subscribe({

        next: data => {

          this.countries =
            data.filter(x => x.isActive);

        },

        error: error => {

          console.error(
            'Country API Error:',
            error
          );

        }

      });


    this.interestFrequencyService
      .getAll()
      .subscribe({

        next: data => {

          this.interestFrequencies = data;

        },

        error: error => {

          console.error(
            'Interest Frequency API Error:',
            error
          );

        }

      });


    this.dayCountConventionService
      .getAll()
      .subscribe({

        next: data => {

          this.dayCountConventions = data;

        },

        error: error => {

          console.error(
            'Day Count API Error:',
            error
          );

        }

      });


    /*
      Load FD only after core data is loaded.
    */

    if (this.isEdit && this.fdId) {

      this.loadFDDetails();

    }

    this.loading = false;

  }


  /* =========================
     LOAD FD DETAILS
  ========================= */

  loadFDDetails(): void {
    if (!this.fdId) return;
    this.loading = true;

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
        const fd = result.general;
        this.fixedDeposit = {
          fdReferenceNo: fd.fdReferenceNo ?? '',
          entityId: fd.entityId ?? null,
          counterpartyId: fd.counterpartyId ?? null,
          currencyCode: fd.currencyCode ?? '',
          principalAmount: fd.principalAmount ?? null,
          startDate: this.formatDate(fd.startDate),
          endDate: this.formatDate(fd.endDate),
          settlementDate: this.formatDate(fd.settlementDate),
          remarks: fd.remarks ?? '',
          status: fd.status ?? ''
        };

        this.interestData = result.interest;
        this.cashFlows = result.cashFlows;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading FD details:', error);
        alert('Unable to load FD details.');
        this.loading = false;
      }
    });
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

  /* =========================
     SAVE / UPDATE
  ========================= */

  save(): void {

    const payload = {

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
        this.fixedDeposit.remarks

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
            alert('Fixed Deposit updated successfully.');
            this.isGeneralReadOnly = true;
            this.loadFDDetails();
          },

          error: error => {

            console.error(
              'FD update error:',
              error
            );

            alert(
              'Unable to update Fixed Deposit.'
            );

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

            this.isEdit = true;

            alert(
              'Fixed Deposit created successfully.'
            );

            /*
              After General Save,
              open Interest tab.
            */

            this.activeTab =
              'interest';

          },

          error: error => {

            console.error(
              'FD create error:',
              error
            );

            alert(
              'Unable to create Fixed Deposit.'
            );

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

  onInterestSaved(interestData: any): void {
    if (!this.fdId) return;
    
    alert('Interest configuration and associated Cash Flows generated successfully.');
    this.activeTab = 'cashflow';
    this.loadFDDetails();
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