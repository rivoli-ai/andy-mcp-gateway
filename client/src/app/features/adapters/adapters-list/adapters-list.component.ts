import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MarkdownModule } from 'ngx-markdown';
import { McpAdapterService } from '../../../core/services/mcp-adapter.service';
import { McpAdapter, AdapterStatus, AdapterType } from '../../../core/models/mcp-adapter.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { NotificationService } from '../../../shared/components/notification/notification.service';
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

  constructor(
    private adapterService: McpAdapterService,
    private notificationService: NotificationService
  ) {}

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

  setView(mode: 'grid' | 'list'): void {
    this.viewMode = mode;
  }

  refreshData(): void {
    this.loadAdapters();
  }

  exportAdapters(): void {
    this.isLoading = true;
    this.adapterService.exportAdaptersToExcel().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `MCP_Adapters_Export_${new Date().toISOString().split('T')[0]}.xlsx`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.isLoading = false;
        this.notificationService.success(
          'Export Successful',
          `${this.filteredAdapters.length} adapter(s) exported to Excel file`
        );
      },
      error: (error) => {
        console.error('Error exporting adapters:', error);
        this.notificationService.error(
          'Export Failed',
          error.message || 'Failed to export adapters. Please try again.'
        );
        this.isLoading = false;
      }
    });
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (file) {
      this.importAdapters(file);
    }
    event.target.value = '';
  }

  importAdapters(file: File): void {
    if (!file.name.endsWith('.xlsx')) {
      this.notificationService.error(
        'Invalid File Type',
        'Please select an Excel (.xlsx) file'
      );
      return;
    }

    this.isLoading = true;
    this.adapterService.importAdaptersFromExcel(file).subscribe({
      next: (result) => {
        this.isLoading = false;
        
        const details: string[] = [];
        if (result.validationErrors && result.validationErrors.length > 0) {
          details.push('Validation Warnings: ' + result.validationErrors.length);
        }
        if (result.failedAdapters && result.failedAdapters.length > 0) {
          details.push('Failed: ' + result.failedCount);
        }
        const detailMessage = details.length > 0 ? details.join(', ') : 'All adapters imported successfully';
        
        if (result.successCount > 0 && result.failedCount === 0) {
          this.notificationService.success(
            'Import Successful',
            `${result.successCount} adapter(s) imported. ${detailMessage}`
          );
        } else if (result.successCount > 0) {
          this.notificationService.warning(
            'Import Partially Successful',
            `${result.successCount} adapter(s) imported. ${detailMessage}`
          );
        } else {
          this.notificationService.error(
            'Import Failed',
            `No adapters were imported. ${detailMessage}`
          );
        }
        
        if (result.validationErrors && result.validationErrors.length > 0) {
          console.warn('Validation errors:', result.validationErrors);
        }
        if (result.failedAdapters && result.failedAdapters.length > 0) {
          console.error('Failed adapters:', result.failedAdapters);
        }
        
        if (result.successCount > 0) {
          this.loadAdapters();
        }
      },
      error: (error) => {
        console.error('Error importing adapters:', error);
        this.notificationService.error(
          'Import Failed',
          error.message || 'Failed to import adapters. Please try again.'
        );
        this.isLoading = false;
      }
    });
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