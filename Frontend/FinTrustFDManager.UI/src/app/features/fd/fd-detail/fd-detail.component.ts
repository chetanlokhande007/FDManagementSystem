import {
  Component,
  OnInit
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import {
  ActivatedRoute,
  Router
} from '@angular/router';

import {
  FDIdentificationService
} from '../../../core/services/fd-identification.service';

import {
  Currency,
  CurrencyService
} from '../../../services/currency.service';


@Component({
  selector: 'app-fd-detail',

  standalone: true,

  imports: [
    CommonModule,
    ReactiveFormsModule
  ],

  templateUrl: './fd-detail.component.html',

  styleUrl: './fd-detail.component.css'
})
export class FDDetailComponent implements OnInit {

  fdForm!: FormGroup;

  fdId: number | null = null;

  isEdit = false;

  loading = false;

  // Currency list from database
  currencies: Currency[] = [];


  constructor(

    private fb: FormBuilder,

    private fdService:
      FDIdentificationService,

    private currencyService:
      CurrencyService,

    private route:
      ActivatedRoute,

    private router: Router

  ) {}


  ngOnInit(): void {

    this.createForm();

    // Load currencies from database
    this.loadCurrencies();


    const id =
      this.route.snapshot.paramMap
        .get('id');


    if (id) {

      this.fdId =
        Number(id);

      this.isEdit = true;

      this.loadFD(
        this.fdId
      );

    }

  }


  // ==============================
  // LOAD CURRENCIES
  // ==============================

  loadCurrencies(): void {

    this.currencyService
      .getCurrencies()
      .subscribe({

        next: (data: Currency[]) => {

          // Only active currencies
          this.currencies =
            data.filter(
              (currency: Currency) =>
                currency.isActive
            );

        },

        error: (error: any) => {

          console.error(
            'Failed to load currencies',
            error
          );

        }

      });

  }


  // ==============================
  // FORM
  // ==============================

  createForm(): void {

    this.fdForm =
      this.fb.group({

        fdId: [
          null
        ],

        fdReferenceNo: [
          ''
        ],

        entityId: [
          '',
          Validators.required
        ],

        counterpartyId: [
          '',
          Validators.required
        ],

        // Do not hardcode INR
        currencyCode: [
          '',
          Validators.required
        ],

        principalAmount: [
          '',
          [
            Validators.required,
            Validators.min(1)
          ]
        ],

        startDate: [
          '',
          Validators.required
        ],

        endDate: [
          '',
          Validators.required
        ],

        settlementDate: [
          '',
          Validators.required
        ],

        status: [
          'DRAFT'
        ],

        remarks: [
          ''
        ]

      });

  }


  // ==============================
  // GET FD
  // ==============================

  loadFD(id: number): void {

    this.loading = true;


    this.fdService
      .getById(id)
      .subscribe({

        next: (data) => {

          this.fdForm.patchValue(data);

          this.loading = false;

        },

        error: (error) => {

          console.error(
            'Error loading FD',
            error
          );

          this.loading = false;

        }

      });

  }


  // ==============================
  // SAVE
  // ==============================

  save(): void {

    if (
      this.fdForm.invalid
    ) {

      this.fdForm.markAllAsTouched();

      return;

    }


    const data =
      this.fdForm.value;


    // ==========================
    // EDIT
    // ==========================

    if (
      this.isEdit &&
      this.fdId
    ) {

      this.fdService
        .update(
          this.fdId,
          data
        )
        .subscribe({

          next: () => {

            alert(
              'FD updated successfully'
            );

            this.router.navigate([
              '/fd'
            ]);

          },

          error: (error) => {

            console.error(
              'Update failed',
              error
            );

          }

        });


      return;

    }


    // ==========================
    // CREATE
    // ==========================

    this.fdService
      .create(data)
      .subscribe({

        next: () => {

          alert(
            'FD created successfully'
          );

          this.router.navigate([
            '/fd'
          ]);

        },

        error: (error) => {

          console.error(
            'Create FD failed',
            error
          );

        }

      });

  }


  // ==============================
  // CANCEL
  // ==============================

  cancel(): void {

    this.router.navigate([
      '/fd'
    ]);

  }


  // ==============================
  // GO BACK
  // ==============================

  goBack(): void {

    this.router.navigate([
      '/fd'
    ]);

  }

}