import { Component, Input, OnInit, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FDCashFlowService, FDCashFlow, FDCashFlowSummary } from '../../../core/services/fd-cash-flow.service';

@Component({
  selector: 'app-fd-cashflow',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './fd-cashflow.component.html',
  styleUrls: ['./fd-cashflow.component.scss']
})
export class FdCashflowComponent implements OnInit, OnChanges {
  @Input() fdId!: number | string;

  summary: FDCashFlowSummary | null = null;
  cashFlows: FDCashFlow[] = [];
  isLoading: boolean = false;
  errorMessage: string = '';

  constructor(
    private cashFlowService: FDCashFlowService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (this.fdId) this.loadData();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['fdId'] && changes['fdId'].currentValue) {
      this.loadData();
    }
  }

  loadData(): void {
    const id = Number(this.fdId);
    if (!id || isNaN(id)) return;

    this.isLoading = true;
    this.errorMessage = '';

    this.cashFlowService.getByFdId(id).subscribe({
      next: (res: FDCashFlowSummary) => {
        this.summary = res;
        this.cashFlows = res.schedule || [];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Failed to load cash flow details.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  formatBasis(basis: string): string {
    return basis ? basis.replace('_', '/') : 'ACTUAL/365';
  }

  getBadgeClass(event: string): string {
    switch (event) {
      case 'FD Created': return 'badge-outflow';
      case 'Compounding Interest': return 'badge-reinvest';
      case 'Maturity': return 'badge-maturity';
      default: return 'badge-accrual';
    }
  }
}