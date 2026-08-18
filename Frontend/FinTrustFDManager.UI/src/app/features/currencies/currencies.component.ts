import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  Currency,
  CurrencyService
} from '../../services/currency.service';

@Component({
  selector: 'app-currencies',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './currencies.component.html',
  styleUrls: ['./currencies.component.css']
})
export class CurrenciesComponent implements OnInit {

  currencies: Currency[] = [];

  showForm = false;
  isEdit = false;
  loading = false;

  currency: Currency = this.emptyCurrency();
  errorMessage: string = '';

  constructor(
    private currencyService: CurrencyService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadCurrencies();
  }

  loadCurrencies(): void {
    // 1. Instant Load from Cache
    const cachedData = sessionStorage.getItem('FINTRUST_CURRENCIES_CACHE');
    if (cachedData) {
      this.currencies = JSON.parse(cachedData);
      this.cdr.detectChanges();
    } else {
      this.loading = true;
      this.cdr.detectChanges();
    }

    // 2. Background Fetch
    this.currencyService.getCurrencies().subscribe({
      next: (data) => {
        sessionStorage.setItem('FINTRUST_CURRENCIES_CACHE', JSON.stringify(data ?? []));
        this.currencies = data ?? [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load currencies', error);
        if (!cachedData) {
          this.currencies = [];
        }
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  emptyCurrency(): Currency {
    return {
      currencyId: 0,
      currencyName: '',
      currencyCode: '',
      symbol: '',
      description: '',
      isActive: true
    };
  }

  addCurrency(): void {

    this.isEdit = false;

    this.currency = this.emptyCurrency();

    this.showForm = true;
  }

  editCurrency(currency: Currency): void {

    this.isEdit = true;

    this.currency = { ...currency };

    this.showForm = true;
  }

  saveCurrency(): void {

    if (!this.currency.currencyName || !this.currency.currencyCode) {
      this.errorMessage = 'Please fill all required fields';
      return;
    }
    this.errorMessage = '';

    const request = {
      currencyName: this.currency.currencyName,
      currencyCode: this.currency.currencyCode,
      symbol: this.currency.symbol,
      description: this.currency.description,
      isActive: this.currency.isActive
    };

    // UPDATE
    if (this.isEdit) {

      this.currencyService
        .updateCurrency(this.currency.currencyId, request)
        .subscribe({

          next: () => {

            this.closeForm();

            // Reload from backend
            this.loadCurrencies();
          },

          error: (error) => {

            console.error('Update failed', error);

            this.errorMessage = error.error || error.message || 'Failed to update currency';
          }
        });

      return;
    }

    // CREATE
    this.currencyService
      .createCurrency(request)
      .subscribe({

        next: () => {

          this.closeForm();

          // Reload from backend
          this.loadCurrencies();
        },

        error: (error) => {

          console.error('Create failed', error);

          this.errorMessage = error.error || error.message || 'Failed to create currency';
        }
      });
  }

  deleteCurrency(id: number): void {

    const confirmed = confirm(
      'Are you sure you want to delete this currency?'
    );

    if (!confirmed) {
      return;
    }

    this.currencyService
      .deleteCurrency(id)
      .subscribe({

        next: () => {

          // Reload from backend
          this.loadCurrencies();
        },

        error: (error) => {

          console.error('Delete failed', error);

          this.errorMessage = 'Failed to delete currency';
        }
      });
  }

  closeForm(): void {

    this.showForm = false;
    this.errorMessage = '';
    this.currency = this.emptyCurrency();
  }
}