import { Component, OnInit, ChangeDetectorRef, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Country, CountryService } from '../../services/country.service';

@Component({
  selector: 'app-countries',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './countries.component.html',
  styleUrls: ['./countries.component.css']
})
export class CountriesComponent implements OnInit {

  countries: Country[] = [];

  showForm = false;
  isEdit = false;
  loading = false;

  country: Country = this.emptyCountry();

  constructor(
    private countryService: CountryService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadCountries();
  }

  loadCountries(): void {
    // 1. Instant Load from Cache
    const cachedData = sessionStorage.getItem('FINTRUST_COUNTRIES_CACHE');
    if (cachedData) {
      this.countries = JSON.parse(cachedData);
      this.cdr.detectChanges();
    } else {
      this.loading = true;
      this.cdr.detectChanges();
    }

    // 2. Background Fetch
    this.countryService.getCountries().subscribe({
      next: (data) => {
        sessionStorage.setItem('FINTRUST_COUNTRIES_CACHE', JSON.stringify(data));
        this.countries = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load countries', error);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  emptyCountry(): Country {
    return {
      countryId: 0,
      countryName: '',
      countryCode: '',
      description: '',
      isActive: true
    };
  }

  addCountry(): void {
    this.isEdit = false;
    this.country = this.emptyCountry();
    this.showForm = true;
  }

  editCountry(country: Country): void {
    this.isEdit = true;
    this.country = { ...country };
    this.showForm = true;
  }

  saveCountry(): void {

    if (
      !this.country.countryName ||
      !this.country.countryCode
    ) {
      alert('Please fill all required fields');
      return;
    }

    const request = {
      countryName: this.country.countryName,
      countryCode: this.country.countryCode,
      description: this.country.description,
      isActive: this.country.isActive
    };

    // UPDATE
    if (this.isEdit) {

      this.countryService
        .updateCountry(this.country.countryId, request)
        .subscribe({
          next: () => {
            this.closeForm();

            // Reload from backend
            this.loadCountries();
          },

          error: (error) => {
            console.error('Update failed', error);
            const msg = error.error || error.message || 'Unknown error';
            alert('Failed to update country: ' + msg);
          }
        });

      return;
    }

    // CREATE
    this.countryService
      .createCountry(request)
      .subscribe({
        next: () => {
          this.closeForm();

          // Reload from backend
          this.loadCountries();
        },

        error: (error) => {
          console.error('Create failed', error);
          const msg = error.error || error.message || 'Unknown error';
          alert('Failed to create country: ' + msg);
        }
      });
  }

  deleteCountry(id: number): void {

    const confirmed = confirm(
      'Are you sure you want to delete this country?'
    );

    if (!confirmed) {
      return;
    }

    this.countryService
      .deleteCountry(id)
      .subscribe({
        next: () => {

          // Reload from backend
          this.loadCountries();
        },

        error: (error) => {
          console.error('Delete failed', error);
          alert('Failed to delete country');
        }
      });
  }

  closeForm(): void {
    this.showForm = false;
    this.country = this.emptyCountry();
  }
}
