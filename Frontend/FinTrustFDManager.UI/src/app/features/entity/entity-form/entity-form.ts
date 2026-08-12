import { Component, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import {
  EntityService,
  CreateEntityDto,
  UpdateEntityDto
} from '../../../core/services/entity.service';

import {
  CountryService,
  Country
} from '../../../core/services/country.service';

import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-entity-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
  templateUrl: './entity-form.html',
  styleUrls: ['./entity-form.css']
})
export class EntityFormComponent implements OnInit {

  entityForm!: FormGroup;

  isEditMode = false;
  entityId: number = 0;

  // Countries will come from Country API
  countries: Country[] = [];

  constructor(
    private fb: FormBuilder,
    private entityService: EntityService,
    private countryService: CountryService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {

    this.initForm();

    // Load countries from Country API
    this.loadCountries();

    this.route.paramMap.subscribe(params => {

      const id = params.get('id');

      if (id) {

        this.isEditMode = true;
        this.entityId = +id;

        this.loadEntityData();
      }
    });
  }


  initForm(): void {

    this.entityForm = this.fb.group({

      entityCode: [
        '',
        [
          Validators.required,
          Validators.maxLength(20)
        ]
      ],

      entityName: [
        '',
        [
          Validators.required,
          Validators.maxLength(150)
        ]
      ],

      countryId: [
        '',
        Validators.required
      ],

      description: [
        '',
        Validators.maxLength(500)
      ]

    });
  }


  // Get countries from Country API
  loadCountries(): void {

    this.countryService.getCountries().subscribe({

      next: (data: Country[]) => {

        console.log('API Countries response:', data);
        this.countries = data.filter(
          (country: any) => country.isActive === true || country.IsActive === true || String(country.isActive).toLowerCase() === 'true' || String(country.IsActive).toLowerCase() === 'true'
        );

      },

      error: (error: any) => {

        console.error(
          'Failed to load countries:',
          error
        );

      }

    });
  }


  loadEntityData(): void {

    this.entityService
      .getById(this.entityId)
      .subscribe({

        next: (data) => {

          this.entityForm.patchValue({

            entityCode: data.entityCode,

            entityName: data.entityName,

            countryId: data.countryId,

            description: data.description

          });

        },

        error: (error: any) => {

          console.error(
            'Failed to load entity:',
            error
          );

        }

      });
  }


  onSubmit(): void {

    if (this.entityForm.invalid) {

      this.entityForm.markAllAsTouched();

      return;
    }

    const formValue = this.entityForm.value;


    // UPDATE
    if (this.isEditMode) {

      const updateData: UpdateEntityDto = {

        entityCode: formValue.entityCode,

        entityName: formValue.entityName,

        countryId: Number(formValue.countryId),

        description: formValue.description

      };

      this.entityService
        .update(this.entityId, updateData)
        .subscribe({

          next: () => {

            this.router.navigate(['/entities']);

          },

          error: (error: any) => {

            console.error(
              'Failed to update entity:',
              error
            );

          }

        });

    }


    // CREATE
    else {

      const createData: CreateEntityDto = {

        entityCode: formValue.entityCode,

        entityName: formValue.entityName,

        countryId: Number(formValue.countryId),

        description: formValue.description

      };

      this.entityService
        .create(createData)
        .subscribe({

          next: () => {

            this.router.navigate(['/entities']);

          },

          error: (error: any) => {

            console.error(
              'Failed to create entity:',
              error
            );

          }

        });
    }
  }


  cancel(): void {

    this.router.navigate(['/entities']);

  }

}