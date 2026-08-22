import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy
} from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
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
export class FdCashflowComponent implements OnChanges {

  @Input() fdId!: number;
  @Input() fdData: any = null;
  @Input() interestData: any = null;
  @Input() cashFlows: FDCashFlow[] = [];

  totalInterest = 0;
  maturityAmount = 0;
  enrichedCashFlows: FDCashFlow[] = [];

  constructor() { }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['cashFlows']) {
      this.calculateCashFlowSummary();
    }
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
