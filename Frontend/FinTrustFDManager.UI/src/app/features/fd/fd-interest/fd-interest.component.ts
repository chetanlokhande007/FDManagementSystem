import { Component, Input, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { FDInterestService } from '../../../core/services/fd-interest.service';

@Component({
  selector: 'app-fd-interest',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './fd-interest.component.html',
  styleUrls: ['./fd-interest.component.css']
})
export class FdInterestComponent implements OnInit {

  @Input() fdId!: number;
  @Input() interestData: any = null;
  @Output() interestSaved = new EventEmitter<any>();

  interestForm!: FormGroup;
  isEdit = false;
  isReadOnly = false;

  constructor(
    private fb: FormBuilder,
    private fdInterestService: FDInterestService
  ) {}

  ngOnInit(): void {
    this.createForm();
    if (this.interestData) {
      this.isEdit = true;
      this.isReadOnly = true;
      this.populateForm(this.interestData);
    }
  }

  createForm(): void {
    this.interestForm = this.fb.group({
      fdInterestId: [0],
      fdId: [this.fdId],
      interestRateType: ['FIXED'],
      interestRate: [0],
      benchmarkName: [''],
      benchmarkRate: [0],
      margin: [0],
      interestFrequency: ['QUARTERLY'],
      compoundingFrequency: ['QUARTERLY'],
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
  }

  edit(): void {
    this.isReadOnly = false;
  }

  saveInterest(): void {
    if (this.interestForm.invalid) return;

    const data = this.interestForm.value;
    // ensure fdId is set correctly
    data.fdId = this.fdId;

    if (this.isEdit && data.fdInterestId) {
      this.fdInterestService.update(data.fdInterestId, data).subscribe({
        next: (res: any) => {
          console.log('Interest updated:', res);
          this.interestSaved.emit(data);
        },
        error: (err: any) => console.error('Error updating interest', err)
      });
    } else {
      this.fdInterestService.create(data).subscribe({
        next: (res: any) => {
          console.log('Interest created:', res);
          this.isEdit = true;
          this.interestForm.patchValue({ fdInterestId: res.fdInterestId });
          this.interestSaved.emit(data);
        },
        error: (err: any) => console.error('Error creating interest', err)
      });
    }
  }
}
