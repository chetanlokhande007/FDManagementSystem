import { Component, Input, OnInit, OnChanges, SimpleChanges, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FDInterestService, FDInterest } from '../../../core/services/fd-interest.service';

@Component({
  selector: 'app-fd-interest',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './fd-interest.component.html',
  styleUrls: ['./fd-interest.component.css']
})
export class FdInterestComponent implements OnInit, OnChanges {

  @Input() fdId!: number;
  @Input() interestData: any = null;
  @Input() interestFrequencies: any[] = [];
  @Output() interestSaved = new EventEmitter<any>();

  /** Frequencies valid for compounding — excludes "At Maturity" */
  get compoundingFrequencies(): any[] {
    return this.interestFrequencies.filter(
      f => f.frequencyName?.toUpperCase() !== 'AT MATURITY'
    );
  }

  interestForm!: FormGroup;
  isEdit = false;
  isReadOnly = false;
  isSaving = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private fdInterestService: FDInterestService
  ) { }

  createForm(): void {
    this.interestForm = this.fb.group({
      fdInterestId: [0],
      fdId: [this.fdId],
      interestRateType: ['FIXED', Validators.required],
      interestRate: [0, [Validators.required, Validators.min(0.01)]],
      benchmarkName: [''],
      benchmarkRate: [0],
      margin: [0],
      interestFrequency: ['', Validators.required],
      compoundingFrequency: [''],
      isCompounding: [false],
      calculationBasis: ['ACTUAL_365', Validators.required],
      paymentConvention: ['']
    });
  }

  populateForm(interest: any): void {
    this.interestForm.patchValue({
      fdInterestId: interest.fdInterestId,
      fdId: interest.fdId,
      interestRateType: interest.interestRateType,
      interestRate: interest.interestRate,
      benchmarkName: interest.benchmarkName,
      benchmarkRate: interest.benchmarkRate,
      margin: interest.margin,
      interestFrequency: interest.interestFrequency,
      compoundingFrequency: interest.compoundingFrequency,
      isCompounding: interest.isCompounding || false,
      calculationBasis: interest.calculationBasis,
      paymentConvention: interest.paymentConvention
    }, { emitEvent: true });
    this.toggleCompoundingFrequency(interest.isCompounding || false);
  }

  ngOnInit(): void {
    this.createForm();
    if (this.interestData) {
      this.isEdit = true;
      this.isReadOnly = true;
      this.populateForm(this.interestData);
    } else {
      this.toggleCompoundingFrequency(false);
    }

    this.interestForm.get('isCompounding')?.valueChanges.subscribe(val => {
      this.toggleCompoundingFrequency(val);
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.interestForm) {
      return; // form not created yet, ngOnInit will handle it
    }

    if (changes['interestData'] || changes['interestFrequencies']) {
      if (this.interestData) {
        this.isEdit = true;
        this.isReadOnly = true;
        this.populateForm(this.interestData);
      } else {
        this.toggleCompoundingFrequency(false);
      }
    }
  }

  toggleCompoundingFrequency(isCompounding: boolean): void {
    const compoundingControl = this.interestForm.get('compoundingFrequency');
    if (!compoundingControl) return;

    if (isCompounding) {
      compoundingControl.enable();
      compoundingControl.setValidators([Validators.required]);
      if (!compoundingControl.value || compoundingControl.value === 'NOT_APPLICABLE') {
        const intFreq = this.interestForm.get('interestFrequency')?.value;
        if (intFreq && intFreq !== 'AT_MATURITY') {
          compoundingControl.setValue(intFreq);
        } else {
          const defaultFreq = this.interestFrequencies.find(f => f.frequencyName?.toUpperCase() === 'QUARTERLY');
          compoundingControl.setValue(defaultFreq ? defaultFreq.frequencyName : (this.interestFrequencies.length ? this.interestFrequencies[0].frequencyName : ''));
        }
      }
    } else {
      compoundingControl.clearValidators();
      compoundingControl.disable();
      compoundingControl.setValue('NOT_APPLICABLE');
    }
    compoundingControl.updateValueAndValidity();
  }

  edit(): void {
    this.isReadOnly = false;
  }

  saveInterest(): void {
    if (this.interestForm.invalid || this.isSaving) {
      return;
    }
    
    this.isSaving = true;
    this.errorMessage = '';

    const data: FDInterest = {
      ...this.interestForm.getRawValue(),
      fdId: this.fdId
    };

    if (this.isEdit && data.fdInterestId) {
      this.fdInterestService
        .update(data.fdInterestId, data)
        .subscribe({
          next: (res: FDInterest) => {
            this.isSaving = false;
            this.interestSaved.emit(res);
          },
          error: (err) => {
            console.error('Error updating interest:', err);
            this.errorMessage = err.error?.message || err.message || 'Failed to update interest configuration.';
            this.isSaving = false;
          }
        });
    } else {
      this.fdInterestService
        .create(data)
        .subscribe({
          next: (res: FDInterest) => {
            this.isEdit = true;
            this.isSaving = false;
            this.interestForm.patchValue({
              fdInterestId: res.fdInterestId
            });
            this.interestSaved.emit(res);
          },
          error: (err) => {
            console.error('Error creating interest:', err);
            this.errorMessage = err.error?.message || err.message || 'Failed to save interest configuration.';
            this.isSaving = false;
          }
        });
    }
  }
}
