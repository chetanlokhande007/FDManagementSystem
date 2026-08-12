import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { EntityService, EntityDto } from '../../core/services/entity.service';

@Component({
  selector: 'app-cash-flow',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cash-flow.html',
  styleUrls: ['./cash-flow.css']
})
export class CashFlowComponent implements OnInit {
  entityId: number = 0;
  entity: EntityDto | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private entityService: EntityService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.entityId = +id;
      this.loadEntity(this.entityId);
    }
  }

  loadEntity(id: number): void {
    this.entityService.getById(id).subscribe(data => {
      this.entity = data;
    });
  }

  goBack(): void {
    this.router.navigate(['/entities']);
  }
}
