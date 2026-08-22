import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy,
  ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import {
  FDCashFlowService, FDCashFlow
} from '../../../core/services/fd-cash-flow.service';

@Component({
  selector: 'app-fd-cashflow',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
  templateUrl: './fd-cashflow.component.html',
  styleUrls: ['./fd-cashflow.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FdCashflowComponent implements OnInit, OnChanges {

  @Input() fdId!: number;
  @Input() fdData: any = null;
  @Input() interestData: any = null;
  @Input() initialCashFlows: FDCashFlow[] = [];
  @Output() cashFlowSaved = new EventEmitter<void>();

  cashFlows: FDCashFlow[] = [];
  enrichedCashFlows: FDCashFlow[] = [];
  totalInterest = 0;
  maturityAmount = 0;
  isLoading = false;
  errorMessage = '';

  constructor(
    private cashFlowService: FDCashFlowService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    // Use pre-loaded cash flows from parent if available (avoids duplicate API call)
    if (this.initialCashFlows && this.initialCashFlows.length > 0) {
      this.cashFlows = this.initialCashFlows;
      this.calculateCashFlowSummary();
    } else {
      this.loadCashFlows();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['initialCashFlows'] && !changes['initialCashFlows'].isFirstChange()) {
      this.cashFlows = changes['initialCashFlows'].currentValue ?? [];
      this.calculateCashFlowSummary();
    }
    if (changes['fdId'] && !changes['fdId'].isFirstChange()) {
      this.loadCashFlows();
    }
  }

  private loadCashFlows(): void {
    if (!this.fdId) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.cashFlows = [];
    this.cdr.markForCheck();

    this.cashFlowService.getByFdId(this.fdId).subscribe({
      next: (response) => {
        this.cashFlows = response ?? [];
        this.calculateCashFlowSummary();
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error loading cash flows', err);
        this.errorMessage = 'Unable to load cash flows.';
        this.cashFlows = [];
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private calculateCashFlowSummary(): void {
    this.enrichedCashFlows = this.cashFlows || [];
    
    this.totalInterest = this.enrichedCashFlows.reduce(
      (sum, cf) => sum + (cf.interestAmount || 0),
      0
    );

    const maturityFlow = this.enrichedCashFlows.find(cf => cf.event === 'Maturity');
    this.maturityAmount = maturityFlow ? maturityFlow.cashFlowAmount : 0;
  }

  trackByCashFlowId(index: number, cashFlow: FDCashFlow): number {
    return cashFlow.cashFlowId;
  }
}
