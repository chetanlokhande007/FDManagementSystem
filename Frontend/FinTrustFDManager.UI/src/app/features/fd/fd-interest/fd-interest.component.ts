import { Component, Input, OnInit, OnChanges, SimpleChanges, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
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

  interestForm!: FormGroup;
  isEdit = false;
  isReadOnly = false;

  constructor(
    private fb: FormBuilder,
    private fdInterestService: FDInterestService
  ) { }

  createForm(): void {
    this.interestForm = this.fb.group({
      fdInterestId: [0],
      fdId: [this.fdId],
      interestRateType: ['FIXED'],
      interestRate: [0],
      benchmarkName: [''],
      benchmarkRate: [0],
      margin: [0],
      interestFrequency: [''],
      compoundingFrequency: [''],
      isCompounding: [false],
      calculationBasis: ['ACTUAL_365'],
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
    });
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
    if (isCompounding) {
      compoundingControl?.enable();
      if (!compoundingControl?.value) {
        const intFreq = this.interestForm.get('interestFrequency')?.value;
        if (intFreq) {
          compoundingControl?.setValue(intFreq);
        } else {
          // Find Quarterly or default to the first one available
          const defaultFreq = this.interestFrequencies.find(f => f.frequencyName?.toUpperCase() === 'QUARTERLY');
          compoundingControl?.setValue(defaultFreq ? defaultFreq.frequencyName : (this.interestFrequencies.length ? this.interestFrequencies[0].frequencyName : ''));
        }
      }
    } else {
      compoundingControl?.disable();
      compoundingControl?.setValue('');
    }
  }

  edit(): void {
    this.isReadOnly = false;
  }

  saveInterest(): void {
    if (this.interestForm.invalid) {
      return;
    }

    const data: FDInterest = {
      ...this.interestForm.getRawValue(),
      fdId: this.fdId
    };

    console.log('Sending FD Interest:', data);

    if (this.isEdit && data.fdInterestId) {
      this.fdInterestService
        .update(data.fdInterestId, data)
        .subscribe({
          next: (res: FDInterest) => {
            console.log('Interest updated:', res);
            this.interestSaved.emit(res);
          },
          error: (err) => {
            console.error('Error updating interest:', err);
          }
        });
    } else {
      this.fdInterestService
        .create(data)
        .subscribe({
          next: (res: FDInterest) => {
            console.log('Interest created:', res);
            this.isEdit = true;
            this.interestForm.patchValue({
              fdInterestId: res.fdInterestId
            });
            this.interestSaved.emit(res);
          },
          error: (err) => {
            console.error('Error creating interest:', err);
          }
        });
    }
  }
}
