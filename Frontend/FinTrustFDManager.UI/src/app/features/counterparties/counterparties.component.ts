import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CounterParty, CounterPartyService } from '../../services/counterparties.service';
import { Country, CountryService } from '../../services/country.service';

@Component({
  selector: 'app-counterparties',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './counterparties.component.html',
  styleUrls: ['./counterparties.component.css']
})
export class CounterpartiesComponent implements OnInit {

  counterParties: CounterParty[] = [];
  countries: Country[] = [];

  showForm = false;
  isEdit = false;
  loading = false;

  counterParty: CounterParty = this.emptyCounterParty();

  constructor(
    private counterPartyService: CounterPartyService,
    private countryService: CountryService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadCounterParties();
    this.loadCountries();
  }

  loadCountries(): void {
    this.countryService.getCountries().subscribe({
      next: (data) => {
        const countries = data ?? [];
        this.countries = countries.filter(c => c.isActive === true || String(c.isActive).toLowerCase() === 'true');
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load countries', error);
      }
    });
  }

  loadCounterParties(): void {
    // 1. Instant Load from Cache
    const cachedData = sessionStorage.getItem('FINTRUST_COUNTERPARTIES_CACHE');
    if (cachedData) {
      this.counterParties = JSON.parse(cachedData);
      this.cdr.detectChanges();
    } else {
      this.loading = true;
      this.cdr.detectChanges();
    }

    // 2. Background Fetch
    this.counterPartyService.getCounterParties().subscribe({
      next: (data) => {
        sessionStorage.setItem('FINTRUST_COUNTERPARTIES_CACHE', JSON.stringify(data ?? []));
        this.counterParties = data ?? [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load counterparties', error);
        if (!cachedData) {
          this.counterParties = [];
        }
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  emptyCounterParty(): CounterParty {
    return {
      counterPartyId: 0,
      counterPartyCode: '',
      counterPartyName: '',
      countryId: 0,
      isActive: true
    };
  }

  addCounterParty(): void {

    this.isEdit = false;

    this.counterParty = this.emptyCounterParty();

    this.showForm = true;
  }

  editCounterParty(counterParty: CounterParty): void {

    this.isEdit = true;

    this.counterParty = { ...counterParty };

    this.showForm = true;
  }

  saveCounterParty(): void {

    if (
      !this.counterParty.counterPartyName ||
      !this.counterParty.counterPartyCode ||
      !this.counterParty.countryId
    ) {
      alert('Please fill all required fields');
      return;
    }

    const request = {
      counterPartyCode: this.counterParty.counterPartyCode,
      counterPartyName: this.counterParty.counterPartyName,
      countryId: this.counterParty.countryId,
      isActive: this.counterParty.isActive
    };

    // UPDATE
    if (this.isEdit) {

      this.counterPartyService
        .updateCounterParty(this.counterParty.counterPartyId, request)
        .subscribe({

          next: () => {

            this.closeForm();

            // Reload from backend
            this.loadCounterParties();
          },

          error: (error) => {

            console.error('Update failed', error);

            const msg =
              error.error ||
              error.message ||
              'Unknown error';

            alert('Failed to update counterparty: ' + msg);
          }
        });

      return;
    }

    // CREATE
    this.counterPartyService
      .createCounterParty(request)
      .subscribe({

        next: () => {

          this.closeForm();

          // Reload from backend
          this.loadCounterParties();
        },

        error: (error) => {

          console.error('Create failed', error);

          const msg =
            error.error ||
            error.message ||
            'Unknown error';

          alert('Failed to create counterparty: ' + msg);
        }
      });
  }

  deleteCounterParty(id: number): void {

    const confirmed = confirm(
      'Are you sure you want to delete this counterparty?'
    );

    if (!confirmed) {
      return;
    }

    this.counterPartyService
      .deleteCounterParty(id)
      .subscribe({

        next: () => {

          // Reload from backend
          this.loadCounterParties();
        },

        error: (error) => {

          console.error('Delete failed', error);

          alert('Failed to delete counterparty');
        }
      });
  }

  closeForm(): void {

    this.showForm = false;

    this.counterParty = this.emptyCounterParty();
  }
}