import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { JobsService, JobDto, ReviewsService, CreateReviewDto } from '@core/api';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-submit-review',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './submit-review.component.html',
  styleUrls: ['./submit-review.component.scss']
})
export class SubmitReviewComponent implements OnInit {
  private jobsService = inject(JobsService);
  private reviewsService = inject(ReviewsService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  currentUser$ = this.authService.currentUser$;
  job: JobDto | null = null;
  reviewForm: FormGroup;
  isLoading = true;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';
  jobId: string | null = null;

  // Star rating states
  overallRatingHover = 0;

  constructor() {
    this.reviewForm = this.fb.group({
      rating: [0, [Validators.required, Validators.min(1), Validators.max(5)]],
      comment: ['', [Validators.maxLength(1000)]]
    });
  }

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('jobId');
    if (this.jobId) {
      this.loadJobDetails(this.jobId);
    } else {
      this.errorMessage = 'Job ID not provided';
      this.isLoading = false;
    }
  }

  loadJobDetails(jobId: string): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.jobsService.apiJobsIdGet(jobId).subscribe({
      next: (job: JobDto) => {
        // Verify this job belongs to the current user
        this.currentUser$.subscribe(user => {
          if (job.customerEmail !== user?.email) {
            this.errorMessage = 'You do not have permission to review this job';
            this.isLoading = false;
            return;
          }

          if (job.status !== 'completed') {
            this.errorMessage = 'You can only review completed jobs';
            this.isLoading = false;
            return;
          }

          this.job = job;
          this.isLoading = false;
        });
      },
      error: (error) => {
        console.error('Error loading job details:', error);
        this.errorMessage = 'Failed to load job details';
        this.isLoading = false;
      }
    });
  }

  setRating(category: string, rating: number): void {
    if (category === 'overall') {
      this.reviewForm.patchValue({ rating });
    }
  }

  getRating(category: string): number {
    if (category === 'overall') {
      return this.reviewForm.get('rating')?.value || 0;
    }
    return 0;
  }

  setHover(category: string, rating: number): void {
    if (category === 'overall') {
      this.overallRatingHover = rating;
    }
  }

  clearHover(category: string): void {
    if (category === 'overall') {
      this.overallRatingHover = 0;
    }
  }

  getHoverRating(category: string): number {
    if (category === 'overall') {
      return this.overallRatingHover;
    }
    return 0;
  }

  getStarClass(category: string, starNumber: number): string {
    const rating = this.getRating(category);
    const hover = this.getHoverRating(category);
    const displayRating = hover > 0 ? hover : rating;

    if (starNumber <= displayRating) {
      return 'star-filled';
    } else {
      return 'star-empty';
    }
  }

  onSubmit(): void {
    if (this.reviewForm.invalid || !this.job?.id) {
      this.errorMessage = 'Please provide an overall rating (1-5 stars)';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const formValue = this.reviewForm.value;
    const createReviewDto: CreateReviewDto = {
      revieweeId: this.job.driverId || null,
      revieweeName: this.job.driverName || null,
      revieweeType: 'Driver',
      jobId: this.job.id,
      rating: formValue.rating,
      comment: formValue.comment || null
    };

    this.reviewsService.apiReviewsPost(createReviewDto).subscribe({
      next: () => {
        this.successMessage = 'Thank you for your review! Your feedback helps us improve our service.';
        this.isSubmitting = false;

        // Redirect to job details after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/customer/jobs', this.jobId]);
        }, 2000);
      },
      error: (error) => {
        console.error('Error submitting review:', error);
        if (error.error?.message) {
          this.errorMessage = error.error.message;
        } else if (error.status === 400) {
          this.errorMessage = 'Invalid review data. Please check your input and try again.';
        } else if (error.status === 404) {
          this.errorMessage = 'Job not found.';
        } else {
          this.errorMessage = 'Failed to submit review. Please try again.';
        }
        this.isSubmitting = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/customer/jobs', this.jobId]);
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric'
    });
  }
}
