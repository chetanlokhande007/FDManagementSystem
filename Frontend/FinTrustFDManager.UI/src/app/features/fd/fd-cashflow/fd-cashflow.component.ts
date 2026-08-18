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
export class FdCashflowComponent implements OnInit {

  @Input() fdId!: number;
  @Input() fdData: any = null;
  @Input() interestData: any = null;
  @Input() cashFlows: FDCashFlow[] = [];
  @Output() cashFlowSaved = new EventEmitter<void>();

  cashFlowForm!: FormGroup;
  showForm = false;
  isEdit = false;

  constructor(
    private fb: FormBuilder,
    private cashFlowService: FDCashFlowService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.createForm();
  }

  createForm(): void {
    this.cashFlowForm = this.fb.group({
      cashFlowId: [0],
      fdId: [this.fdId],
      cashFlowDate: [''],
      cashFlowType: [''],
      direction: [''],
      principalAmount: [0],
      grossInterest: [0],
      tdsAmount: [0],
      netInterest: [0],
      totalAmount: [0],
      currencyCode: [''],
      status: ['PENDING'],
      referenceNo: ['']
    });
  }

  addCashFlow(): void {
    this.isEdit = false;
    this.createForm();
    this.showForm = true;
  }

  editCashFlow(cf: FDCashFlow): void {
    this.isEdit = true;
    this.showForm = true;
    const formatDt = (dt: string) => dt ? new Date(dt).toISOString().split('T')[0] : '';
    
    this.cashFlowForm.patchValue({
      cashFlowId: cf.cashFlowId,
      fdId: cf.fdId,
      cashFlowDate: formatDt(cf.cashFlowDate),
      cashFlowType: cf.cashFlowType,
      direction: cf.direction,
      principalAmount: cf.principalAmount,
      grossInterest: cf.grossInterest,
      tdsAmount: cf.tdsAmount,
      netInterest: cf.netInterest,
      totalAmount: cf.totalAmount,
      currencyCode: cf.currencyCode,
      status: cf.status,
      referenceNo: cf.referenceNo
    });
  }

  deleteCashFlow(id: number): void {
    if (confirm('Are you sure you want to delete this cash flow?')) {
      this.cashFlowService.delete(id).subscribe({
        next: () => {
          this.cashFlowSaved.emit();
        },
        error: (err: any) => console.error(err)
      });
    }
  }

  saveCashFlow(): void {
    if (this.cashFlowForm.invalid) {
      return;
    }
    const data = this.cashFlowForm.value;
    data.fdId = this.fdId;
    
    if (this.isEdit && data.cashFlowId > 0) {
      this.cashFlowService.update(data.cashFlowId, data).subscribe({
        next: (res: any) => {
          console.log('Cash flow updated:', res);
          this.showForm = false;
          this.cashFlowSaved.emit();
        },
        error: (err: any) => {
          console.error('Error updating cash flow', err);
        }
      });
    } else {
      this.cashFlowService.create(data).subscribe({
        next: (res: any) => {
          console.log('Cash flow created:', res);
          this.showForm = false;
          this.cashFlowSaved.emit();
        },
        error: (err: any) => {
          console.error('Error creating cash flow', err);
        }
      });
    }
  }

  cancel(): void {
    this.showForm = false;
  }

  get enrichedCashFlows() {
    if (!this.cashFlows || this.cashFlows.length === 0) return [];
    
    return this.cashFlows.map((cf, index) => {
      let startDate = this.fdData?.startDate;
      if (index > 0) {
        startDate = this.cashFlows[index - 1].cashFlowDate;
      }
      
      let endDate = cf.cashFlowDate;
      if (cf.cashFlowType === 'PRINCIPAL' && this.cashFlows.length > 1) {
         endDate = this.cashFlows[1].cashFlowDate; 
      }
      
      return {
        ...cf,
        periodStart: startDate,
        periodEnd: endDate
      };
    });
  }

  get totalInterest(): number {
    return this.cashFlows
      .filter(cf => cf.cashFlowType === 'INTEREST')
      .reduce((sum, cf) => sum + cf.grossInterest, 0);
  }

  get maturityAmount(): number {
    const maturityFlow = this.cashFlows.find(cf => cf.cashFlowType === 'MATURITY');
    return maturityFlow ? maturityFlow.totalAmount : 0;
  }
}
