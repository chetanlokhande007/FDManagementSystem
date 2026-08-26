import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FDCashFlowService, FDCashFlow, FDCashFlowSummary } from '../../../core/services/fd-cash-flow.service';

@Component({
  selector: 'app-fd-cashflow',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './fd-cashflow.component.html',
  styleUrls: ['./fd-cashflow.component.css']
})
export class FdCashflowComponent implements OnInit, OnChanges {
  @Input() fdId!: number;
  @Input() initialCashFlowSummary: any = null;
  @Input() fdData: any = null;
  @Input() interestData: any = null;
  @Output() cashFlowSaved = new EventEmitter<void>();

  cashFlows: FDCashFlow[] = [];
  principalAmount: number = 0;
  totalInterest: number = 0;
  maturityAmount: number = 0;
  isLoading: boolean = false;
  errorMessage: string = '';

  constructor(private cashFlowService: FDCashFlowService) { }

  ngOnInit(): void {
    if (this.initialCashFlowSummary && this.initialCashFlowSummary.cashFlows && this.initialCashFlowSummary.cashFlows.length > 0) {
      this.applySummary(this.initialCashFlowSummary);
    } else if (this.fdId) {
      this.loadData();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['initialCashFlowSummary'] && !changes['initialCashFlowSummary'].isFirstChange()) {
      this.applySummary(changes['initialCashFlowSummary'].currentValue);
    }

    if (changes['fdId'] && !changes['fdId'].isFirstChange()) {
      this.loadData();
    }
  }

  private applySummary(summary: any): void {
    if (!summary) {
      this.cashFlows = [];
      this.totalInterest = 0;
      this.maturityAmount = 0;
      this.principalAmount = this.fdData?.principalAmount || 0;
      return;
    }

    this.cashFlows = summary.cashFlows || [];
    this.totalInterest = summary.totalInterest || 0;
    this.maturityAmount = summary.maturityAmount || 0;

    // Principal can come from the parent's fdData, or extracted from the "FD Created" event.
    const principalFlow = this.cashFlows.find(cf => cf.event === 'FD Created');
    this.principalAmount = principalFlow ? principalFlow.cashFlowAmount : (this.fdData?.principalAmount || 0);
  }

  loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.cashFlowService.getByFdId(this.fdId).subscribe({
      next: (summary: FDCashFlowSummary) => {
        this.applySummary(summary);
        this.isLoading = false;
      },
      error: (err: any) => {
        console.error(err);
        this.errorMessage = 'Unable to load cash flow records.';
        this.applySummary(null);
        this.isLoading = false;
      }
    });
  }

  getBadgeClass(event: string): string {
    switch (event) {
      case 'FD Created': return 'badge-outflow';
      case 'Compounding Interest': return 'badge-reinvest';
      case 'Maturity': return 'badge-maturity';
      default: return 'badge-accrual';
    }
  }

  get currencyCode(): string {
    return this.fdData?.currencyCode || 'INR';
  }

  trackByCashFlowId(index: number, item: FDCashFlow): number {
    return item.cashFlowId;
  }
}