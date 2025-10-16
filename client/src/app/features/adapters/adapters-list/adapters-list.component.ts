import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MarkdownModule } from 'ngx-markdown';
import { McpAdapterService } from '../../../core/services/mcp-adapter.service';
import { McpAdapter, AdapterStatus, AdapterType } from '../../../core/models/mcp-adapter.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
@Component({
  selector: 'app-adapters-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MarkdownModule,
    LoadingSpinnerComponent,
    StatusBadgeComponent
  ],
  templateUrl: './adapters-list.component.html',
  styleUrl: './adapters-list.component.scss'
})
export class AdaptersListComponent implements OnInit {
  dataSource: McpAdapter[] = [];
  filteredAdapters: McpAdapter[] = [];
  
  searchTerm = '';
  statusFilter = '';
  typeFilter = '';
  viewMode: 'grid' | 'list' = 'grid';
  isLoading = false;

  constructor(private adapterService: McpAdapterService) {}

  ngOnInit(): void {
    this.loadAdapters();
  }

  loadAdapters(): void {
    this.isLoading = true;
    this.adapterService.getAllAdapters().subscribe({
      next: (data) => {
        this.dataSource = data.adapters;
        this.applyFilters();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading adapters:', error);
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    this.filteredAdapters = this.dataSource.filter(adapter => {
      const matchesSearch = !this.searchTerm || 
        adapter.name.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        adapter.url.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        (adapter.description && adapter.description.toLowerCase().includes(this.searchTerm.toLowerCase()));

      const matchesStatus = !this.statusFilter || this.getStatusMatch(adapter);
      const matchesType = !this.typeFilter || this.getTypeMatch(adapter);

      return matchesSearch && matchesStatus && matchesType;
    });
  }

  private getStatusMatch(adapter: McpAdapter): boolean {
    switch (this.statusFilter) {
      case 'healthy':
        return adapter.isHealthy === true;
      case 'unhealthy':
        return adapter.isHealthy === false;
      case 'disabled':
        return !adapter.enabled;
      default:
        return true;
    }
  }

  private getTypeMatch(adapter: McpAdapter): boolean {
    switch (this.typeFilter) {
      case 'StreamableHttp':
        return (adapter.type as any) === AdapterType.StreamableHttp || (adapter.type as any) === 1;
      case 'Sse':
        return (adapter.type as any) === AdapterType.Sse || (adapter.type as any) === 2;
      default:
        return true;
    }
  }

  onSearch(): void {
    this.applyFilters();
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  toggleView(): void {
    this.viewMode = this.viewMode === 'grid' ? 'list' : 'grid';
  }

  refreshData(): void {
    this.loadAdapters();
  }

  checkHealth(adapterId: string): void {
    console.log('Checking health for adapter:', adapterId);
    // TODO: Implement health check functionality
  }

  deleteAdapter(adapterId: string): void {
    if (confirm('Are you sure you want to delete this adapter?')) {
      this.adapterService.deleteAdapter(adapterId).subscribe({
        next: () => {
          this.loadAdapters();
        },
        error: (error) => {
          console.error('Error deleting adapter:', error);
        }
      });
    }
  }
}