import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DocumentsService, DocumentViewModel } from '@core/api';

@Component({
  selector: 'app-my-documents',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="page-header">
        <div>
          <h1>My Documents</h1>
          <p class="subtitle">Upload and manage your driver documents</p>
        </div>
      </div>

      <!-- Upload Section -->
      <div class="bg-white border-2 border-dashed border-gray-300 rounded-lg p-8 mb-6 hover:border-blue-500 transition-colors">
        <div class="text-center">
          <svg class="w-12 h-12 mx-auto text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
          </svg>
          <h3 class="mt-4 text-lg font-medium text-gray-900">Upload Document</h3>
          <p class="mt-2 text-sm text-gray-600">Driver's License, Insurance, Registration, etc.</p>

          <div class="mt-4">
            <label class="inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg cursor-pointer transition-colors">
              <input
                type="file"
                (change)="onFileSelected($event)"
                accept="image/*,.pdf"
                class="hidden"
              />
              <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              Choose File
            </label>
          </div>

          <!-- Selected File Info -->
          <div *ngIf="selectedFile" class="mt-4 text-sm text-gray-600">
            <p>Selected: <span class="font-medium">{{ selectedFile.name }}</span></p>
          </div>

          <!-- Upload Form -->
          <div *ngIf="selectedFile" class="mt-6 max-w-md mx-auto space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Document Type *</label>
              <select
                [(ngModel)]="documentType"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="">Select type...</option>
                <option value="DriversLicense">Driver's License</option>
                <option value="Insurance">Insurance</option>
                <option value="Registration">Vehicle Registration</option>
                <option value="Inspection">Vehicle Inspection</option>
                <option value="Other">Other</option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Expiry Date (if applicable)</label>
              <input
                type="date"
                [(ngModel)]="expiryDate"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            <div class="flex gap-3">
              <button
                (click)="uploadDocument()"
                [disabled]="isUploading || !documentType"
                class="flex-1 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {{ isUploading ? 'Uploading...' : 'Upload Document' }}
              </button>
              <button
                (click)="cancelUpload()"
                class="px-4 py-2 text-gray-700 hover:text-gray-900 font-medium"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Error/Success Messages -->
      <div *ngIf="successMessage" class="bg-green-50 border border-green-200 rounded-lg p-4 mb-6">
        <p class="text-green-800">{{ successMessage }}</p>
      </div>
      <div *ngIf="errorMessage" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading documents...</p>
      </div>

      <!-- Documents List -->
      <div *ngIf="!isLoading" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div *ngIf="documents.length === 0" class="col-span-full text-center py-12">
          <svg class="w-16 h-16 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <p class="mt-4 text-gray-600">No documents uploaded yet</p>
        </div>

        <div
          *ngFor="let doc of documents"
          class="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-md transition-shadow"
        >
          <!-- Document Icon -->
          <div class="flex items-start justify-between mb-4">
            <div
              class="w-12 h-12 rounded-lg flex items-center justify-center"
              [ngClass]="{
                'bg-green-100 text-green-600': doc.status === 'Approved' || doc.status === 'Verified',
                'bg-yellow-100 text-yellow-600': doc.status === 'Pending',
                'bg-red-100 text-red-600': doc.status === 'Rejected',
                'bg-gray-100 text-gray-600': !doc.status
              }"
            >
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <span
              class="px-2 py-1 text-xs font-semibold rounded-full"
              [ngClass]="{
                'bg-green-100 text-green-800': doc.status === 'Approved' || doc.status === 'Verified',
                'bg-yellow-100 text-yellow-800': doc.status === 'Pending',
                'bg-red-100 text-red-800': doc.status === 'Rejected',
                'bg-gray-100 text-gray-800': !doc.status
              }"
            >
              {{ doc.status || 'Pending' }}
            </span>
          </div>

          <!-- Document Info -->
          <h3 class="text-lg font-semibold text-gray-900 mb-2">{{ formatDocumentType(doc.type) }}</h3>
          <div class="space-y-1 text-sm text-gray-600 mb-4">
            <p>Uploaded: {{ formatDate(doc.uploadedDate) }}</p>
            <p *ngIf="doc.expiryDate">
              Expires: <span [class.text-red-600]="isExpiringSoon(doc.expiryDate)">{{ formatDate(doc.expiryDate) }}</span>
            </p>
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-2">
            <button
              (click)="viewDocument(doc)"
              class="text-sm text-blue-600 hover:text-blue-700 font-medium"
            >
              View
            </button>
            <button
              (click)="deleteDocument(doc.id!)"
              class="text-sm text-red-600 hover:text-red-700 font-medium ml-auto"
            >
              Delete
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      padding: 2rem;
      background: linear-gradient(135deg, #003d82 0%, #001f3f 100%);
      border-radius: 1rem;
      color: white;
    }
    .page-header h1 {
      margin: 0 0 0.5rem 0;
      font-size: 2rem;
      font-weight: 700;
    }
    .page-header .subtitle {
      margin: 0;
      opacity: 0.9;
      font-size: 1rem;
    }
    @media (max-width: 768px) {
      .page-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1.5rem;
      }
    }
  `]
})
export class MyDocumentsComponent implements OnInit {
  private documentsService = inject(DocumentsService);

  documents: DocumentViewModel[] = [];
  isLoading = false;
  isUploading = false;
  errorMessage = '';
  successMessage = '';

  selectedFile: File | null = null;
  documentType = '';
  expiryDate = '';

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.documentsService.apiDriversMeDocumentsGet().subscribe({
      next: (documents: DocumentViewModel[]) => {
        this.documents = documents || [];
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading documents:', error);
        this.errorMessage = 'Failed to load documents. Please try again.';
        this.isLoading = false;
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.selectedFile = input.files[0];
      this.errorMessage = '';
      this.successMessage = '';
    }
  }

  uploadDocument(): void {
    if (!this.selectedFile || !this.documentType) {
      this.errorMessage = 'Please select a file and document type';
      return;
    }

    this.isUploading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.documentsService.apiDriversMeDocumentsPost(
      this.documentType,
      this.expiryDate || undefined,
      this.selectedFile
    ).subscribe({
      next: () => {
        this.successMessage = 'Document uploaded successfully!';
        this.isUploading = false;
        this.cancelUpload();
        this.loadDocuments();
      },
      error: (error: any) => {
        console.error('Error uploading document:', error);
        this.errorMessage = 'Failed to upload document. Please try again.';
        this.isUploading = false;
      }
    });
  }

  cancelUpload(): void {
    this.selectedFile = null;
    this.documentType = '';
    this.expiryDate = '';
  }

  deleteDocument(documentId: string): void {
    if (!confirm('Are you sure you want to delete this document?')) {
      return;
    }

    this.documentsService.apiDriversMeDocumentsIdDelete(documentId).subscribe({
      next: () => {
        this.successMessage = 'Document deleted successfully';
        this.loadDocuments();
      },
      error: (error: any) => {
        console.error('Error deleting document:', error);
        this.errorMessage = 'Failed to delete document. Please try again.';
      }
    });
  }

  viewDocument(doc: DocumentViewModel): void {
    if (doc.fileUrl) {
      // Open document in new tab
      window.open(doc.fileUrl, '_blank');
    } else {
      this.errorMessage = 'Document file not available';
    }
  }

  formatDocumentType(type: string | null | undefined): string {
    if (!type) return 'Document';
    return type.replace(/([A-Z])/g, ' $1').trim();
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  isExpiringSoon(expiryDate: string | null | undefined): boolean {
    if (!expiryDate) return false;
    const expiry = new Date(expiryDate);
    const today = new Date();
    const daysUntilExpiry = Math.floor((expiry.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    return daysUntilExpiry <= 30 && daysUntilExpiry >= 0;
  }
}
