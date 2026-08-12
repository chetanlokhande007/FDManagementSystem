import { Component, OnInit } from '@angular/core';
import { EntityDto, EntityService } from '../../../core/services/entity.service';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-entity-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './entity-list.html',
  styleUrls: ['./entity-list.css']
})
export class EntityListComponent implements OnInit {
  entities: EntityDto[] = [];
  searchTerm: string = '';

  constructor(private entityService: EntityService, private router: Router) {}

  ngOnInit(): void {
    this.loadEntities();
  }

  loadEntities(): void {
    this.entityService.getAll().subscribe(data => {
      this.entities = data;
    });
  }

  get filteredEntities() {
    return this.entities.filter(e => {
      const nameMatch = e.entityName ? e.entityName.toLowerCase().includes(this.searchTerm.toLowerCase()) : false;
      const codeMatch = e.entityCode ? e.entityCode.toLowerCase().includes(this.searchTerm.toLowerCase()) : false;
      return nameMatch || codeMatch;
    });
  }

  addEntity(): void {
    this.router.navigate(['/entities/add']);
  }

  editEntity(id: number): void {
    this.router.navigate(['/entities/edit', id]);
  }

  deleteEntity(id: number): void {
    if (confirm('Are you sure you want to delete this entity?')) {
      this.entityService.delete(id).subscribe(() => {
        this.loadEntities();
      });
    }
  }

  approveEntity(id: number): void {
    this.entityService.approve(id).subscribe(() => {
      this.loadEntities();
    });
  }

  rejectEntity(id: number): void {
    this.entityService.reject(id).subscribe(() => {
      this.loadEntities();
    });
  }

  openCashFlow(entityId: number): void {
    this.router.navigate(['/entities', entityId, 'cash-flow']);
  }
}
