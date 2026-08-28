import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  BenchmarkService,
  Benchmark,
  BenchmarkRateHistoryService,
  BenchmarkRateHistory
} from '../../core/services/benchmark.service';

@Component({
  selector: 'app-benchmark',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './benchmark.component.html',
  styleUrls: ['./benchmark.component.css']
})
export class BenchmarkComponent implements OnInit {

  benchmarks: Benchmark[] = [];

  showForm = false;
  isEdit = false;
  loading = false;
  errorMessage = '';

  benchmark: Benchmark = this.emptyBenchmark();

  // Rate History
  selectedBenchmark: Benchmark | null = null;
  rateHistory: BenchmarkRateHistory[] = [];
  showHistoryForm = false;
  historyForm: BenchmarkRateHistory = this.emptyHistory();

  constructor(
    private benchmarkService: BenchmarkService,
    private benchmarkRateHistoryService: BenchmarkRateHistoryService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadBenchmarks();
  }

  loadBenchmarks(): void {
    const cachedData = sessionStorage.getItem('FINTRUST_BENCHMARKS_CACHE');
    if (cachedData) {
      this.benchmarks = JSON.parse(cachedData);
      this.cdr.detectChanges();
    } else {
      this.loading = true;
      this.cdr.detectChanges();
    }

    this.benchmarkService.getAll().subscribe({
      next: (data) => {
        sessionStorage.setItem('FINTRUST_BENCHMARKS_CACHE', JSON.stringify(data));
        this.benchmarks = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load benchmarks', error);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  emptyBenchmark(): Benchmark {
    return {
      benchmarkId: 0,
      benchmarkName: '',
      description: '',
      currentRate: 0,
      rateUnit: '%',
      isActive: true
    };
  }

  emptyHistory(): BenchmarkRateHistory {
    const today = new Date();
    const todayStr = today.toISOString().split('T')[0];
    return {
      benchmarkRateHistoryId: 0,
      benchmarkId: 0,
      rate: 0,
      effectiveFrom: todayStr,
      effectiveTo: undefined
    };
  }

  addBenchmark(): void {
    this.isEdit = false;
    this.benchmark = this.emptyBenchmark();
    this.showForm = true;
  }

  editBenchmark(benchmark: Benchmark): void {
    this.isEdit = true;
    this.benchmark = { ...benchmark };
    this.showForm = true;
  }

  saveBenchmark(): void {
    if (!this.benchmark.benchmarkName) {
      this.errorMessage = 'Benchmark Name is required.';
      return;
    }
    this.errorMessage = '';

    const request: Partial<Benchmark> = {
      benchmarkName: this.benchmark.benchmarkName,
      description: this.benchmark.description,
      currentRate: this.benchmark.currentRate,
      rateUnit: this.benchmark.rateUnit || '%',
      isActive: this.benchmark.isActive
    };

    if (this.isEdit) {
      this.benchmarkService
        .update(this.benchmark.benchmarkId, request)
        .subscribe({
          next: () => {
            this.closeForm();
            this.loadBenchmarks();
          },
          error: (error) => {
            this.errorMessage = error.error?.message || error.message || 'Failed to update benchmark.';
          }
        });
      return;
    }

    this.benchmarkService
      .create(request)
      .subscribe({
        next: () => {
          this.closeForm();
          this.loadBenchmarks();
        },
        error: (error) => {
          this.errorMessage = error.error?.message || error.message || 'Failed to create benchmark.';
        }
      });
  }

  deleteBenchmark(id: number): void {
    const confirmed = confirm('Are you sure you want to delete this benchmark?');
    if (!confirmed) return;

    this.benchmarkService.delete(id).subscribe({
      next: () => this.loadBenchmarks(),
      error: (error) => {
        console.error('Delete failed', error);
        alert('Failed to delete benchmark.');
      }
    });
  }

  closeForm(): void {
    this.showForm = false;
    this.errorMessage = '';
    this.benchmark = this.emptyBenchmark();
  }

  // ── Rate History ──

  viewHistory(benchmark: Benchmark): void {
    this.selectedBenchmark = benchmark;
    this.loadRateHistory(benchmark.benchmarkId);
  }

  closeHistory(): void {
    this.selectedBenchmark = null;
    this.rateHistory = [];
    this.showHistoryForm = false;
  }

  loadRateHistory(benchmarkId: number): void {
    this.benchmarkRateHistoryService.getByBenchmarkId(benchmarkId).subscribe({
      next: (data) => {
        this.rateHistory = data;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load rate history', error);
        this.rateHistory = [];
      }
    });
  }

  addRateHistory(): void {
    if (!this.selectedBenchmark) return;
    this.historyForm = this.emptyHistory();
    this.historyForm.benchmarkId = this.selectedBenchmark.benchmarkId;
    this.showHistoryForm = true;
  }

  saveRateHistory(): void {
    if (!this.historyForm.rate || !this.historyForm.effectiveFrom) {
      this.errorMessage = 'Rate and Effective From date are required.';
      return;
    }
    this.errorMessage = '';

    const request: Partial<BenchmarkRateHistory> = {
      benchmarkId: this.selectedBenchmark!.benchmarkId,
      rate: this.historyForm.rate,
      effectiveFrom: this.historyForm.effectiveFrom,
      effectiveTo: this.historyForm.effectiveTo || undefined
    };

    this.benchmarkRateHistoryService.create(request).subscribe({
      next: () => {
        this.showHistoryForm = false;
        this.loadRateHistory(this.selectedBenchmark!.benchmarkId);
        // Also refresh the benchmark list to pick up any CurrentRate changes
        this.loadBenchmarks();
      },
      error: (error) => {
        this.errorMessage = error.error?.message || error.message || 'Failed to add rate history.';
      }
    });
  }

  deleteRateHistory(id: number): void {
    const confirmed = confirm('Are you sure you want to delete this rate history entry?');
    if (!confirmed) return;

    this.benchmarkRateHistoryService.delete(id).subscribe({
      next: () => {
        if (this.selectedBenchmark) {
          this.loadRateHistory(this.selectedBenchmark.benchmarkId);
        }
      },
      error: (error) => {
        console.error('Delete failed', error);
        alert('Failed to delete rate history entry.');
      }
    });
  }

  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return 'Present';
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
