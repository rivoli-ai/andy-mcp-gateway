import { Component, OnInit, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { McpAdapterService } from '../../../core/services/mcp-adapter.service';
import { CreateMcpAdapter, UpdateMcpAdapter, McpAdapter, AdapterType, AdapterStatus } from '../../../core/models/mcp-adapter.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { tap } from 'rxjs';
import { NgZone } from '@angular/core';

@Component({
  selector: 'app-adapter-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    LoadingSpinnerComponent
  ],
  templateUrl: './adapter-form.component.html',
  styleUrl: './adapter-form.component.scss'
})
export class AdapterFormComponent implements OnInit {
  @Input() adapterId?: string;

  adapterForm: FormGroup;
  isEditMode = false;
  isLoading = false;
  isSubmitting = false;
  isListingTools = false;
  submitError: string | null = null;
  toolsResult: { success: boolean; error?: string; tools?: any[] } | null = null;
  AdapterType = AdapterType; // Make enum available in template

  constructor(
    private fb: FormBuilder,
    private adapterService: McpAdapterService,
    private route: ActivatedRoute,
    private router: Router
    , private zone: NgZone
  ) {
    this.adapterForm = this.createForm();
  }

  ngOnInit(): void {
    this.adapterId = this.route.snapshot.paramMap.get('id') || undefined;
    this.isEditMode = !!this.adapterId;
    
    if (this.isEditMode) {
      this.loadAdapter();
    }
  }

  createForm(): FormGroup {
    return this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      url: ['', [Validators.required, Validators.pattern(/^https?:\/\/.+/)]],
      description: [''],
      type: ['', [Validators.required]],
      timeoutSeconds: [30, [Validators.min(1), Validators.max(300)]],
      enabled: [true]
    });
  }

  loadAdapter(): void {
    if (!this.adapterId) return;
    
    this.isLoading = true;
    this.adapterService.getAdapterById(this.adapterId).subscribe({
      next: (adapter) => {
        this.adapterForm.patchValue({
          name: adapter.name,
          url: adapter.url,
          description: adapter.description || '',
          type: this.convertStringToEnum(adapter.type),
          timeoutSeconds: adapter.timeoutSeconds,
          enabled: adapter.enabled
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading adapter:', error);
        this.isLoading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.adapterForm.invalid) {
      this.adapterForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.submitError = null;
    const formValue = this.adapterForm.value;

    if (this.isEditMode) {
      const updateData: UpdateMcpAdapter = {
        name: formValue.name,
        url: formValue.url,
        description: formValue.description,
        type: formValue.type,
        timeoutSeconds: formValue.timeoutSeconds,
        enabled: formValue.enabled,
        updatedBy: 'current-user' // This should come from auth service
      };
      
      this.adapterService.updateAdapter(this.adapterId!, updateData).subscribe({
        next: () => {
          this.router.navigate(['/adapters']);
          this.isSubmitting = false;
        },
        error: (error) => {
          console.error('Error updating adapter:', error);
          this.submitError = error.error?.error || error.message || 'Failed to update adapter';
          this.isSubmitting = false;
        }
      });
    } else {
      const createData: CreateMcpAdapter = {
        name: formValue.name,
        url: formValue.url,
        description: formValue.description,
        type: formValue.type,
        timeoutSeconds: formValue.timeoutSeconds,
        enabled: formValue.enabled,
        createdBy: 'current-user' // This should come from auth service
      };
      
      this.adapterService.createAdapter(createData).subscribe({
        next: () => {
          this.router.navigate(['/adapters']);
          this.isSubmitting = false;
        },
        error: (error) => {
          console.error('Error creating adapter:', error);
          this.submitError = error.error?.error || error.message || 'Failed to create adapter';
          this.isSubmitting = false;
        }
      });
    }
  }

  listTools(): void {
    if (this.adapterForm.invalid) {
      return;
    }
  
    const formValue = this.adapterForm.value;
    const adapter: McpAdapter = {
      id: this.adapterId || '',
      name: formValue.name,
      url: formValue.url,
      description: formValue.description,
      type: formValue.type,
      timeoutSeconds: formValue.timeoutSeconds,
      enabled: formValue.enabled,
      isHealthy: false,
      status: AdapterStatus.Unknown,
      createdBy: '',
      updatedBy: '',
      createdAt: new Date(),
      updatedAt: new Date()
    };
  
    this.isListingTools = true;
    this.toolsResult = null;
  
    this.adapterService.testConnection(adapter).subscribe({
      next: (result) => {
        this.zone.run(() => {
          this.toolsResult = result;
          this.isListingTools = false;
        });
      },
      error: (error) => {
        this.zone.run(() => {
          this.toolsResult = { success: false, error: error.message || 'Failed to load tools' };
          this.isListingTools = false;
        });
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/adapters']);
  }

  private convertStringToEnum(typeString: any): AdapterType {
    if (typeof typeString === 'string') {
      return AdapterType[typeString as keyof typeof AdapterType];
    }
    return typeString as AdapterType;
  }

  getSchemaProperties(schema: any): Array<{name: string, type: string, required: boolean}> {
    if (!schema || !schema.properties) return [];
    
    const properties = schema.properties;
    const required = schema.required || [];
    
    return Object.keys(properties).map(propName => ({
      name: propName,
      type: properties[propName].type || 'any',
      required: required.includes(propName)
    }));
  }

  getIntegrationCode(): string {
    const formValue = this.adapterForm.value;
    const adapterName = formValue.name || 'adapter_name';
    const adapterType = this.getAdapterTypeString(formValue.type);
    const apiUrl = 'http://localhost:8080'; // Using user preference for port 8080
    const timeout = formValue.timeoutSeconds || 30;
    const disabled = !formValue.enabled;
    
    // Determine the endpoint based on the type (always use 'sse' for SSE, 'mcp' for StreamableHttp)
    const endpoint = adapterType === 'sse' ? 'sse' : 'mcp';

    return `"${adapterName}": {
  "autoApprove": [],
  "disabled": ${disabled},
  "timeout": ${timeout},
  "type": "${adapterType}",
  "url": "${apiUrl}/adapters/${adapterName}/${endpoint}"
}`;
  }

  private getAdapterTypeString(type: AdapterType | string | number): string {
    // Handle if type is already a string from backend
    if (typeof type === 'string') {
      const typeStr = type.toLowerCase();
      if (typeStr === 'sse' || typeStr.includes('sse')) {
        return 'sse';
      }
      if (typeStr === 'streamablehttp' || typeStr.includes('streamable')) {
        return 'streamable-http';
      }
      return 'sse';
    }
    
    // Handle enum values
    switch (type) {
      case AdapterType.Sse:
      case 2:
        return 'sse';
      case AdapterType.StreamableHttp:
      case 1:
        return 'streamable-http';
      default:
        return 'sse';
    }
  }

  copyIntegrationCode(): void {
    const code = this.getIntegrationCode();
    navigator.clipboard.writeText(code).then(() => {
      // You can add a toast notification here
      console.log('Integration code copied to clipboard');
    }).catch(err => {
      console.error('Failed to copy code:', err);
    });
  }
}