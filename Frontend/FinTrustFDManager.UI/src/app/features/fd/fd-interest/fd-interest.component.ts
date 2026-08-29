import { Component, Input, OnInit, OnChanges, SimpleChanges, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FDInterestService, FDInterest } from '../../../core/services/fd-interest.service';
import { BenchmarkService, Benchmark } from '../../../core/services/benchmark.service';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

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
  @Input() dayCountConventions: any[] = [];
  @Input() benchmarks: Benchmark[] = [];
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
  effectiveRate = 0;

  constructor(
    private fb: FormBuilder,
    private fdInterestService: FDInterestService,
    private benchmarkService: BenchmarkService
  ) { }

  createForm(): void {
    this.interestForm = this.fb.group({
      fdInterestId: [0],
      fdId: [this.fdId],
      interestRateType: ['FIXED', Validators.required],
      interestRate: [0, [Validators.required, Validators.min(0.01)]],
      benchmarkId: [null],
      benchmarkName: [''],
      benchmarkRate: [0],
      margin: [0],
      interestFrequencyId: [null, Validators.required],
      compoundingFrequencyId: [null],
      isCompounding: [false],
      dayCountConventionId: [null, Validators.required],
      paymentConvention: ['']
    });
  }

  loadBenchmarks(): void {
    this.benchmarkService.getAll().pipe(
      catchError(() => of([]))
    ).subscribe(benchmarks => {
      this.benchmarks = benchmarks.filter(b => b.isActive);
    });
  }

  onRateTypeChange(rateType: string): void {
    const isFloating = rateType === 'FLOATING';
    const benchmarkIdControl = this.interestForm.get('benchmarkId');
    const benchmarkRateControl = this.interestForm.get('benchmarkRate');
    const benchmarkNameControl = this.interestForm.get('benchmarkName');
    const marginControl = this.interestForm.get('margin');
    const interestRateControl = this.interestForm.get('interestRate');

    if (isFloating) {
      benchmarkIdControl?.setValidators([Validators.required]);
      marginControl?.setValidators([Validators.required, Validators.min(0)]);
      // Disable manual interest rate for floating
      interestRateControl?.clearValidators();
      interestRateControl?.setValue(0);
    } else {
      benchmarkIdControl?.clearValidators();
      benchmarkIdControl?.setValue(null);
      benchmarkRateControl?.setValue(0);
      benchmarkNameControl?.setValue('');
      marginControl?.clearValidators();
      marginControl?.setValue(0);
      interestRateControl?.setValidators([Validators.required, Validators.min(0.01)]);
      this.effectiveRate = 0;
    }

    benchmarkIdControl?.updateValueAndValidity();
    benchmarkRateControl?.updateValueAndValidity();
    marginControl?.updateValueAndValidity();
    interestRateControl?.updateValueAndValidity();
  }

  onBenchmarkChange(benchmarkId: number): void {
    if (!benchmarkId) {
      this.interestForm.patchValue({
        benchmarkName: '',
        benchmarkRate: 0
      });
      this.calculateEffectiveRate();
      return;
    }

    const selected = this.benchmarks.find(b => b.benchmarkId === benchmarkId);
    if (selected) {
      this.interestForm.patchValue({
        benchmarkName: selected.benchmarkName,
        benchmarkRate: selected.currentRate
      });
      this.calculateEffectiveRate();
    }
  }

  calculateEffectiveRate(): void {
    const rateType = this.interestForm.get('interestRateType')?.value;
    if (rateType === 'FLOATING') {
      const benchmarkRate = this.interestForm.get('benchmarkRate')?.value || 0;
      const margin = this.interestForm.get('margin')?.value || 0;
      this.effectiveRate = benchmarkRate + margin;
    } else {
      this.effectiveRate = this.interestForm.get('interestRate')?.value || 0;
    }
  }

  populateForm(interest: any): void {
    this.interestForm.patchValue({
      fdInterestId: interest.fdInterestId,
      fdId: interest.fdId,
      interestRateType: interest.interestRateType,
      interestRate: interest.interestRate,
      benchmarkId: interest.benchmarkId || null,
      benchmarkName: interest.benchmarkName || '',
      benchmarkRate: interest.benchmarkRate || 0,
      margin: interest.margin || 0,
      interestFrequencyId: interest.interestFrequencyId || null,
      compoundingFrequencyId: interest.compoundingFrequencyId || null,
      isCompounding: interest.isCompounding || false,
      dayCountConventionId: interest.dayCountConventionId || null,
      paymentConvention: interest.paymentConvention || ''
    }, { emitEvent: true });
    this.toggleCompoundingFrequency(interest.isCompounding || false);
    this.calculateEffectiveRate();
  }

  ngOnInit(): void {
    this.createForm();
    if (!this.benchmarks || this.benchmarks.length === 0) {
      this.loadBenchmarks();
    }

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

    // Watch for rate type changes
    this.interestForm.get('interestRateType')?.valueChanges.subscribe(val => {
      this.onRateTypeChange(val);
    });

    // Watch for benchmark selection changes
    this.interestForm.get('benchmarkId')?.valueChanges.subscribe(val => {
      this.onBenchmarkChange(val);
    });

    // Watch for margin changes to recalculate effective rate
    this.interestForm.get('margin')?.valueChanges.subscribe(() => {
      this.calculateEffectiveRate();
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.interestForm) {
      return; // form not created yet, ngOnInit will handle it
    }

    if (changes['benchmarks'] && this.benchmarks && this.benchmarks.length > 0) {
      // Benchmarks loaded from parent, no need to fetch
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
    const compoundingControl = this.interestForm.get('compoundingFrequencyId');
    if (!compoundingControl) return;

    if (isCompounding) {
      compoundingControl.enable();
      compoundingControl.setValidators([Validators.required]);
      if (!compoundingControl.value) {
        const intFreqId = this.interestForm.get('interestFrequencyId')?.value;
        const atMaturityFreq = this.interestFrequencies.find(f => f.frequencyName?.toUpperCase() === 'AT MATURITY');
        if (intFreqId && intFreqId !== atMaturityFreq?.id) {
          compoundingControl.setValue(intFreqId);
        } else {
          const defaultFreq = this.interestFrequencies.find(f => f.frequencyName?.toUpperCase() === 'QUARTERLY');
          compoundingControl.setValue(defaultFreq ? defaultFreq.id : (this.interestFrequencies.length ? this.interestFrequencies[0].id : null));
        }
      }
    } else {
      compoundingControl.clearValidators();
      compoundingControl.disable();
      compoundingControl.setValue(null);
    }
    compoundingControl.updateValueAndValidity();
  }

  edit(): void {
    this.isReadOnly = false;
  }

  cancelEdit(): void {
    this.isReadOnly = true;
    if (this.interestData) {
      this.populateForm(this.interestData);
    }
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
