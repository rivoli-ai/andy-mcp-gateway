import { Component, OnInit, Input, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { McpAdapterService } from '../../../core/services/mcp-adapter.service';
import { CreateMcpAdapter, UpdateMcpAdapter, AdapterType } from '../../../core/models/mcp-adapter.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { IntegrationClientTabIconComponent } from '../../../shared/components/integration-client-tab-icon/integration-client-tab-icon.component';
import { APP_CONFIG, AppConfig } from '../../../core/services/config.service';
import {
  INTEGRATION_CLIENT_OPTIONS,
  IntegrationClientId,
  IntegrationSerde,
  IntegrationSnippetInput,
  buildIntegrationSnippet,
} from '../../../core/utils/mcp-integration-snippets';

@Component({
  selector: 'app-adapter-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    LoadingSpinnerComponent,
    IntegrationClientTabIconComponent,
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
  submitError: string | null = null;
  AdapterType = AdapterType; // Make enum available in template

  readonly integrationClientOptions = INTEGRATION_CLIENT_OPTIONS;
  integrationClient: IntegrationClientId = 'opencode';
  integrationSerde: IntegrationSerde = 'json';

  constructor(
    private fb: FormBuilder,
    private adapterService: McpAdapterService,
    private route: ActivatedRoute,
    private router: Router,
    @Inject(APP_CONFIG) private readonly appConfig: AppConfig
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
      enabled: [true],
      headers: this.fb.array([])
    });
  }

  get headers(): FormArray {
    return this.adapterForm.get('headers') as FormArray;
  }

  addHeader(): void {
    const headerGroup = this.fb.group({
      key: ['', Validators.required],
      value: ['', Validators.required]
    });
    this.headers.push(headerGroup);
  }

  removeHeader(index: number): void {
    this.headers.removeAt(index);
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
        
        // Load headers
        this.headers.clear();
        if (adapter.headers) {
          Object.entries(adapter.headers).forEach(([key, value]) => {
            const headerGroup = this.fb.group({
              key: [key, Validators.required],
              value: [value, Validators.required]
            });
            this.headers.push(headerGroup);
          });
        }
        
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

    // Convert headers array to dictionary
    const headersDict: { [key: string]: string } = {};
    if (formValue.headers && formValue.headers.length > 0) {
      formValue.headers.forEach((header: { key: string; value: string }) => {
        if (header.key && header.value) {
          headersDict[header.key] = header.value;
        }
      });
    }

    if (this.isEditMode) {
      const updateData: UpdateMcpAdapter = {
        name: formValue.name,
        url: formValue.url,
        description: formValue.description,
        type: formValue.type,
        timeoutSeconds: formValue.timeoutSeconds,
        enabled: formValue.enabled,
        headers: Object.keys(headersDict).length > 0 ? headersDict : undefined,
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
        headers: Object.keys(headersDict).length > 0 ? headersDict : undefined,
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

  onCancel(): void {
    this.router.navigate(['/adapters']);
  }

  private convertStringToEnum(typeString: any): AdapterType {
    if (typeof typeString === 'string') {
      return AdapterType[typeString as keyof typeof AdapterType];
    }
    return typeString as AdapterType;
  }

  setIntegrationClient(client: IntegrationClientId): void {
    this.integrationClient = client;
  }

  setIntegrationSerde(serde: IntegrationSerde): void {
    this.integrationSerde = serde;
  }

  getIntegrationCode(): string {
    const formValue = this.adapterForm.value;
    const adapterName = formValue.name || 'adapter_name';
    const adapterType = this.getAdapterTypeString(formValue.type);
    const label: IntegrationSnippetInput['adapterTypeLabel'] =
      adapterType === 'sse' ? 'sse' : 'streamable-http';

    const headersDict: Record<string, string> = {};
    if (formValue.headers && formValue.headers.length > 0) {
      (formValue.headers as Array<{ key: string; value: string }>).forEach((h) => {
        if (h.key && h.value) {
          headersDict[h.key] = h.value;
        }
      });
    }

    const input: IntegrationSnippetInput = {
      adapterName,
      gatewayBaseUrl: this.appConfig.apiUrl,
      adapterTypeLabel: label,
      timeoutSeconds: formValue.timeoutSeconds || 30,
      enabled: !!formValue.enabled,
      headers: Object.keys(headersDict).length ? headersDict : undefined,
    };

    return buildIntegrationSnippet(input, this.integrationClient, this.integrationSerde);
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