import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit
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
  styleUrls: ['./fd-cashflow.component.css']
})
export class FdCashflowComponent {

  @Input() fdId!: number;
  @Input() fdData: any = null;
  @Input() interestData: any = null;
  @Input() cashFlows: FDCashFlow[] = [];

  constructor() {}

  get enrichedCashFlows() {
    return this.cashFlows;
  }

  get totalInterest(): number {
    return this.cashFlows
      .filter(cf => cf.event !== 'FD Created' && cf.event !== 'Maturity' && cf.event !== 'Compounding Interest')
      .reduce((sum, cf) => sum + cf.interestAmount, 0);
  }

  get maturityAmount(): number {
    const maturityFlow = this.cashFlows.find(cf => cf.event === 'Maturity');
    return maturityFlow ? maturityFlow.cashFlowAmount : 0;
  }
}
