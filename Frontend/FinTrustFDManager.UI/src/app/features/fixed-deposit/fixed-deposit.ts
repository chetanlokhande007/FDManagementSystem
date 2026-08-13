import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-fixed-deposit',
  templateUrl: './fixed-deposit.html',
  styleUrl: './fixed-deposit.css',
  imports: [CommonModule, FormsModule]
})
export class FixedDepositComponent {

  activeTab: 'general' | 'interest' | 'cashflow' = 'general';

  fixedDeposit = {
    reference: '',
    entity: '',
    counterparty: '',
    counterpartyType: '',
    transactionCurrency: '',
    transactionAmount: null,
    startDate: '',
    endDate: '',
    settlementDate: '',
    bankAccount: '',
    remarks: ''
  };

  selectTab(tab: 'general' | 'interest' | 'cashflow'): void {
    this.activeTab = tab;
  }

  save(): void {
    console.log('Fixed Deposit:', this.fixedDeposit);
  }

  cancel(): void {
    console.log('Cancel');
  }
}