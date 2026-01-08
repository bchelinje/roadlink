import { Component, inject, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReviewsService, Review } from '@core/api';

@Component({
  selector: 'app-driver-reviews',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './driver-reviews.component.html',
  styleUrls: ['./driver-reviews.component.scss']
})
export class DriverReviewsComponent implements OnInit, OnChanges {
  @Input() driverId!: string;
  @Input() showFullList = false; // If false, show only top 5 reviews

  private reviewsService = inject(ReviewsService);

  reviews: Review[] = [];
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    if (this.driverId) {
      this.loadReviews();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['driverId'] && !changes['driverId'].firstChange) {
      this.loadReviews();
    }
  }

  loadReviews(): void {
    if (!this.driverId) {
      this.errorMessage = 'Driver ID is required';
      this.isLoading = false;
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    // Using the available method to get driver reviews
    this.reviewsService.apiReviewsDriversIdGet(this.driverId).subscribe({
      next: (reviews: Review[]) => {
        this.reviews = reviews;
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading reviews:', error);
        this.errorMessage = 'Failed to load reviews';
        this.isLoading = false;
      }
    });
  }

  getStarArray(rating: number | undefined | null): boolean[] {
    const actualRating = rating || 0;
    const fullStars = Math.floor(actualRating);
    const hasHalfStar = actualRating % 1 >= 0.5;
    const stars: boolean[] = [];

    for (let i = 0; i < 5; i++) {
      stars.push(i < fullStars || (i === fullStars && hasHalfStar));
    }
    return stars;
  }

  getDisplayedReviews(): Review[] {
    if (!this.reviews) return [];
    return this.showFullList
      ? this.reviews
      : this.reviews.slice(0, 5);
  }

  getAverageRating(): number {
    if (!this.reviews || this.reviews.length === 0) return 0;
    const sum = this.reviews.reduce((acc, review) => acc + (review.rating || 0), 0);
    return sum / this.reviews.length;
  }

  formatDate(date: string | undefined): string {
    if (!date) return '';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }
}
