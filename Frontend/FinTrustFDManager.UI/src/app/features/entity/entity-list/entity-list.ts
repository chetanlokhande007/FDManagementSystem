import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Country, CountryService } from '../../../core/services/country.service';
import { EntityDto, EntityService } from '../../../core/services/entity.service';

@Component({
  selector: 'app-entity-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './entity-list.html',
  styleUrls: ['./entity-list.css']
})
export class EntityListComponent implements OnInit {

  entities: EntityDto[] = [];
  countries: Country[] = [];
  
  showForm = false;
  isEdit = false;
  loading = false;

  entity: Partial<EntityDto> = this.emptyEntity();

  constructor(
    private entityService: EntityService,
    private countryService: CountryService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadCountries();
    this.loadEntities();
  }

  loadCountries(): void {
    this.countryService.getCountries().subscribe({
      next: (data) => {
        this.countries = data.filter(c => c.isActive === true || String(c.isActive).toLowerCase() === 'true');
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load countries', error);
      }
    });
  }

  loadEntities(): void {
    // 1. Instant Load from Cache
    const cachedData = sessionStorage.getItem('FINTRUST_ENTITIES_CACHE');
    if (cachedData) {
      this.entities = JSON.parse(cachedData);
      this.cdr.detectChanges();
    } else {
      this.loading = true;
      this.cdr.detectChanges();
    }

    // 2. Background Fetch
    this.entityService.getAll().subscribe({
      next: (data) => {
        sessionStorage.setItem('FINTRUST_ENTITIES_CACHE', JSON.stringify(data));
        this.entities = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load entities', error);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  emptyEntity(): Partial<EntityDto> {
    return {
      entityId: 0,
      entityName: '',
      entityCode: '',
      countryId: 0,
      status: 1 // Default to 1 (Approved/Active)
    };
  }

  addEntity(): void {
    this.isEdit = false;
    this.entity = this.emptyEntity();
    this.showForm = true;
  }

  editEntity(item: EntityDto): void {
    this.isEdit = true;
    this.entity = { ...item };
    this.showForm = true;
  }

  saveEntity(): void {
    if (
      !this.entity.entityName ||
      !this.entity.entityCode ||
      !this.entity.countryId
    ) {
      alert('Please fill all required fields');
      return;
    }

    const request: any = {
      entityName: this.entity.entityName,
      entityCode: this.entity.entityCode,
      countryId: this.entity.countryId,
      status: this.entity.status
    };

    if (this.isEdit && this.entity.entityId) {
      this.entityService
        .update(this.entity.entityId, request)
        .subscribe({
          next: () => {
            this.closeForm();
            this.loadEntities();
          },
          error: (error) => {
            console.error('Update failed', error);
            alert('Failed to update entity');
          }
        });
      return;
    }

    this.entityService
      .create(request)
      .subscribe({
        next: () => {
          this.closeForm();
          this.loadEntities();
        },
        error: (error) => {
          console.error('Create failed', error);
          alert('Failed to create entity');
        }
      });
  }

  deleteEntity(id: number): void {
    const confirmed = confirm('Are you sure you want to delete this entity?');
    if (!confirmed) {
      return;
    }

    this.entityService
      .delete(id)
      .subscribe({
        next: () => {
          this.loadEntities();
        },
        error: (error) => {
          console.error('Delete failed', error);
          alert('Failed to delete entity');
        }
      });
  }

  closeForm(): void {
    this.showForm = false;
    this.entity = this.emptyEntity();
  }
}