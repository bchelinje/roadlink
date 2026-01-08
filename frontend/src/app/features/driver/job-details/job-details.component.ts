// features/driver/job-details/job-details.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DriversService } from '@core/api/api/drivers.service';
import { JobDto, AddNoteDto, CompleteJobDto } from '@core/api/model/models';

@Component({
  selector: 'app-job-details',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './job-details.component.html',
  styleUrls: ['./job-details.component.scss']
})
export class JobDetailsComponent implements OnInit {
  private readonly driversService = inject(DriversService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  job: JobDto | null = null;
  loading = true;
  error: string | null = null;
  actionLoading = false;
  actionError: string | null = null;

  // Note modal
  showNoteModal = false;
  newNote = '';
  noteLoading = false;

  // Completion modal
  showCompletionModal = false;
  completionNotes = '';
  completionLoading = false;

  // Photo upload
  selectedFile: File | null = null;
  uploadProgress = 0;

  ngOnInit(): void {
    const jobId = this.route.snapshot.paramMap.get('id');
    if (jobId) {
      this.loadJobDetails(jobId);
    } else {
      this.error = 'Invalid job ID';
      this.loading = false;
    }
  }

  private loadJobDetails(jobId: string): void {
    this.loading = true;
    this.error = null;

    this.driversService.apiDriversMeJobsJobIdGet(jobId).subscribe({
      next: (job: JobDto) => {
        this.job = job;
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Failed to load job details:', err);
        this.error = 'Failed to load job details. Please try again.';
        this.loading = false;
      }
    });
  }

  acceptJob(): void {
    if (!this.job?.id) return;

    this.actionLoading = true;
    this.actionError = null;

    this.driversService.apiDriversMeJobsJobIdAcceptPost(this.job.id).subscribe({
      next: () => {
        this.actionLoading = false;
        this.loadJobDetails(this.job!.id!);
      },
      error: (err: any) => {
        console.error('Failed to accept job:', err);
        this.actionError = 'Failed to accept job. Please try again.';
        this.actionLoading = false;
      }
    });
  }

  startJob(): void {
    if (!this.job?.id) return;

    this.actionLoading = true;
    this.actionError = null;

    this.driversService.apiDriversMeJobsJobIdStartPost(this.job.id).subscribe({
      next: () => {
        this.actionLoading = false;
        this.loadJobDetails(this.job!.id!);
      },
      error: (err: any) => {
        console.error('Failed to start job:', err);
        this.actionError = 'Failed to start job. Please try again.';
        this.actionLoading = false;
      }
    });
  }

  openCompletionModal(): void {
    this.showCompletionModal = true;
    this.completionNotes = '';
  }

  closeCompletionModal(): void {
    this.showCompletionModal = false;
    this.completionNotes = '';
  }

  completeJob(): void {
    if (!this.job?.id) return;

    this.completionLoading = true;

    const completeDto: CompleteJobDto = {
      notes: this.completionNotes.trim() || 'Job completed successfully'
    };

    this.driversService.apiDriversMeJobsJobIdCompletePost(this.job.id, completeDto).subscribe({
      next: () => {
        this.completionLoading = false;
        this.closeCompletionModal();
        this.loadJobDetails(this.job!.id!);
      },
      error: (err: any) => {
        console.error('Failed to complete job:', err);
        this.completionLoading = false;
        alert('Failed to complete job. Please try again.');
      }
    });
  }

  openNoteModal(): void {
    this.showNoteModal = true;
    this.newNote = '';
  }

  closeNoteModal(): void {
    this.showNoteModal = false;
    this.newNote = '';
  }

  addNote(): void {
    if (!this.job?.id || !this.newNote.trim()) return;

    this.noteLoading = true;

    const noteDto: AddNoteDto = {
      note: this.newNote.trim()
    };

    this.driversService.apiDriversMeJobsJobIdNotesPost(this.job.id, noteDto).subscribe({
      next: () => {
        this.noteLoading = false;
        this.closeNoteModal();
        this.loadJobDetails(this.job!.id!);
      },
      error: (err: any) => {
        console.error('Failed to add note:', err);
        this.noteLoading = false;
        alert('Failed to add note. Please try again.');
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.selectedFile = input.files[0];
      this.uploadPhoto();
    }
  }

  uploadPhoto(): void {
    if (!this.job?.id || !this.selectedFile) return;

    this.driversService.apiDriversMeJobsJobIdPhotosPost(this.job.id, this.selectedFile, 'completion', 'Job photo').subscribe({
      next: () => {
        this.selectedFile = null;
        this.uploadProgress = 0;
        this.loadJobDetails(this.job!.id!);
      },
      error: (err: any) => {
        console.error('Failed to upload photo:', err);
        alert('Failed to upload photo. Please try again.');
        this.selectedFile = null;
        this.uploadProgress = 0;
      }
    });
  }

  canAcceptJob(): boolean {
    return this.job?.status === 'assigned' || this.job?.status === 'pending';
  }

  canStartJob(): boolean {
    return this.job?.status === 'confirmed' || this.job?.status === 'assigned';
  }

  canCompleteJob(): boolean {
    return this.job?.status === 'in_progress';
  }

  getStatusBadgeClass(status: string): string {
    const statusMap: Record<string, string> = {
      'pending': 'bg-yellow-100 text-yellow-800',
      'assigned': 'bg-blue-100 text-blue-800',
      'confirmed': 'bg-purple-100 text-purple-800',
      'in_progress': 'bg-indigo-100 text-indigo-800',
      'completed': 'bg-green-100 text-green-800',
      'cancelled': 'bg-red-100 text-red-800',
      'on_hold': 'bg-gray-100 text-gray-800'
    };
    return statusMap[status] || 'bg-gray-100 text-gray-800';
  }

  getPriorityBadgeClass(priority: string): string {
    const priorityMap: Record<string, string> = {
      'urgent': 'bg-red-100 text-red-800',
      'high': 'bg-orange-100 text-orange-800',
      'normal': 'bg-blue-100 text-blue-800',
      'low': 'bg-gray-100 text-gray-800'
    };
    return priorityMap[priority] || 'bg-gray-100 text-gray-800';
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  formatPriority(priority: string): string {
    return priority.charAt(0).toUpperCase() + priority.slice(1);
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  formatTime(time: string | null | undefined): string {
    if (!time) return 'N/A';
    return new Date(time).toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  }

  formatDateTime(dateTime: string | null | undefined): string {
    if (!dateTime) return 'N/A';
    return new Date(dateTime).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  }

  goBack(): void {
    this.router.navigate(['/driver/jobs']);
  }
}
