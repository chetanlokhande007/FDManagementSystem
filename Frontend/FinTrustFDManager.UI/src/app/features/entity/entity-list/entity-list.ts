import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import {
  EntityDto,
  EntityService
} from '../../../core/services/entity.service';

@Component({
  selector: 'app-entity-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './entity-list.html',
  styleUrls: ['./entity-list.css']
})
export class EntityListComponent implements OnInit {

  entities: EntityDto[] = [];
  filteredEntities: EntityDto[] = [];

  searchText = '';

  loading = false;
  errorMessage = '';

  constructor(
    private entityService: EntityService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadEntities();
  }

  loadEntities(): void {
    this.loading = true;
    this.errorMessage = '';

    console.time('ENTITY API');

    this.entityService
      .getAll()
      .pipe(
        finalize(() => {
          this.loading = false;
          console.timeEnd('ENTITY API');
        })
      )
      .subscribe({
        next: (data: EntityDto[]) => {
          console.log('Entities API response:', data);

          this.entities = data ?? [];
          this.filteredEntities = [...this.entities];
        },

        error: (error: any) => {
          console.error('Failed to load entities:', error);

          this.entities = [];
          this.filteredEntities = [];

          this.errorMessage =
            'Unable to load entities. Please try again.';
        }
      });
  }

  searchEntities(): void {
    const search = this.searchText
      .trim()
      .toLowerCase();

    if (!search) {
      this.filteredEntities = [...this.entities];
      return;
    }

    this.filteredEntities = this.entities.filter(entity =>
      entity.entityCode?.toLowerCase().includes(search) ||
      entity.entityName?.toLowerCase().includes(search) ||
      entity.countryName?.toLowerCase().includes(search)
    );
  }

  addEntity(): void {
    this.router.navigate(['/entities/add']);
  }

  editEntity(id: number): void {
    this.router.navigate(['/entities/edit', id]);
  }

  deleteEntity(id: number): void {
    if (!confirm('Are you sure you want to delete this entity?')) {
      return;
    }

    this.entityService.delete(id).subscribe({
      next: () => {
        this.loadEntities();
      },

      error: (error: any) => {
        console.error('Failed to delete entity:', error);

        this.errorMessage =
          'Unable to delete entity. Please try again.';
      }
    });
  }
}