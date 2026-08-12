import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { EntityService, CreateEntityDto, UpdateEntityDto } from '../../../core/services/entity.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-entity-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './entity-form.html',
  styleUrls: ['./entity-form.css']
})
export class EntityFormComponent implements OnInit {
  entityForm!: FormGroup;
  isEditMode = false;
  entityId: number = 0;
  
  // Dummy countries for now. You can replace this with a real CountryService later.
  countries = [
    { id: 1, name: 'India' },
    { id: 2, name: 'USA' },
    { id: 3, name: 'UK' },
    { id: 4, name: 'Singapore' }
  ];

  constructor(
    private fb: FormBuilder,
    private entityService: EntityService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.initForm();
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
      entityCode: ['', [Validators.required, Validators.maxLength(20)]],
      entityName: ['', [Validators.required, Validators.maxLength(150)]],
      countryId: ['', Validators.required],
      description: ['', Validators.maxLength(500)]
    });
  }

  loadEntityData(): void {
    this.entityService.getById(this.entityId).subscribe(data => {
      this.entityForm.patchValue({
        entityCode: data.entityCode,
        entityName: data.entityName,
        countryId: data.countryId,
        description: data.description
      });
    });
  }

  onSubmit(): void {
    if (this.entityForm.invalid) {
      return;
    }

    const formValue = this.entityForm.value;

    if (this.isEditMode) {
      const updateData: UpdateEntityDto = {
        entityCode: formValue.entityCode,
        entityName: formValue.entityName,
        countryId: Number(formValue.countryId),
        description: formValue.description
      };
      this.entityService.update(this.entityId, updateData).subscribe(() => {
        this.router.navigate(['/entities']);
      });
    } else {
      const createData: CreateEntityDto = {
        entityCode: formValue.entityCode,
        entityName: formValue.entityName,
        countryId: Number(formValue.countryId),
        description: formValue.description
      };
      this.entityService.create(createData).subscribe(() => {
        this.router.navigate(['/entities']);
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/entities']);
  }
}
