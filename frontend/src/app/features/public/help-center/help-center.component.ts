import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HelpCenterService } from '@core/services/help-center.service';
import { FAQ, HelpArticle } from '@core/models/help-center.models';

@Component({
  selector: 'app-help-center',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="text-center mb-12">
        <h1 class="text-4xl font-bold text-gray-800 mb-4">Help Center</h1>
        <p class="text-gray-600 mb-8">Find answers to common questions and helpful guides</p>

        <!-- Search -->
        <div class="max-w-2xl mx-auto">
          <div class="relative">
            <input
              [(ngModel)]="searchQuery"
              (keyup.enter)="search()"
              type="text"
              placeholder="Search for help..."
              class="w-full px-4 py-3 pr-12 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <button
              (click)="search()"
              class="absolute right-2 top-2 px-4 py-1.5 bg-blue-600 text-white rounded-md hover:bg-blue-700">
              Search
            </button>
          </div>
        </div>
      </div>

      <!-- FAQs Section -->
      <div class="mb-12">
        <h2 class="text-2xl font-bold text-gray-800 mb-6">Frequently Asked Questions</h2>

        @if (isLoadingFAQs) {
          <div class="flex justify-center py-12">
            <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
          </div>
        } @else if (faqs.length === 0) {
          <p class="text-gray-500 text-center py-8">No FAQs available</p>
        } @else {
          <div class="space-y-4">
            @for (faq of faqs; track faq.id) {
              <div class="bg-white rounded-lg shadow-md overflow-hidden">
                <button
                  (click)="toggleFAQ(faq.id)"
                  class="w-full px-6 py-4 text-left flex justify-between items-center hover:bg-gray-50">
                  <span class="font-medium text-gray-800">{{ faq.question }}</span>
                  <svg
                    [class.rotate-180]="expandedFAQs.has(faq.id)"
                    class="w-5 h-5 text-gray-500 transition-transform"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                </button>
                @if (expandedFAQs.has(faq.id)) {
                  <div class="px-6 py-4 bg-gray-50 border-t border-gray-200">
                    <p class="text-gray-600">{{ faq.answer }}</p>
                    <div class="mt-4 flex gap-2">
                      <button
                        (click)="voteFAQ(faq.id, true)"
                        class="text-sm text-green-600 hover:text-green-700">
                        Helpful
                      </button>
                      <button
                        (click)="voteFAQ(faq.id, false)"
                        class="text-sm text-red-600 hover:text-red-700">
                        Not Helpful
                      </button>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        }
      </div>

      <!-- Articles Section -->
      <div>
        <h2 class="text-2xl font-bold text-gray-800 mb-6">Help Articles</h2>

        @if (isLoadingArticles) {
          <div class="flex justify-center py-12">
            <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
          </div>
        } @else if (articles.length === 0) {
          <p class="text-gray-500 text-center py-8">No articles available</p>
        } @else {
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            @for (article of articles; track article.id) {
              <div class="bg-white rounded-lg shadow-md overflow-hidden hover:shadow-lg transition-shadow">
                @if (article.coverImageUrl) {
                  <img [src]="article.coverImageUrl" alt="{{ article.title }}" class="w-full h-48 object-cover" />
                }
                <div class="p-6">
                  <h3 class="text-lg font-semibold text-gray-800 mb-2">{{ article.title }}</h3>
                  <p class="text-gray-600 text-sm mb-4">{{ article.excerpt }}</p>
                  <div class="flex justify-between items-center text-sm text-gray-500">
                    <span>{{ article.estimatedReadTime }} min read</span>
                    <a
                      [routerLink]="['/help-center/articles', article.slug]"
                      class="text-blue-600 hover:text-blue-700 font-medium">
                      Read More
                    </a>
                  </div>
                </div>
              </div>
            }
          </div>
        }
      </div>
    </div>
  `
})
export class HelpCenterComponent implements OnInit {
  private helpCenterService = inject(HelpCenterService);

  faqs: FAQ[] = [];
  articles: HelpArticle[] = [];
  searchQuery = '';
  isLoadingFAQs = false;
  isLoadingArticles = false;
  expandedFAQs = new Set<string>();

  ngOnInit(): void {
    this.loadFAQs();
    this.loadArticles();
  }

  loadFAQs(): void {
    this.isLoadingFAQs = true;
    this.helpCenterService.getFAQs({ isPublished: true }).subscribe({
      next: (faqs) => {
        this.faqs = faqs;
        this.isLoadingFAQs = false;
      },
      error: (error) => {
        console.error('Error loading FAQs:', error);
        this.isLoadingFAQs = false;
      }
    });
  }

  loadArticles(): void {
    this.isLoadingArticles = true;
    this.helpCenterService.getArticles({ isPublished: true, pageSize: 6 }).subscribe({
      next: (articles) => {
        this.articles = articles;
        this.isLoadingArticles = false;
      },
      error: (error) => {
        console.error('Error loading articles:', error);
        this.isLoadingArticles = false;
      }
    });
  }

  search(): void {
    if (!this.searchQuery.trim()) {
      this.loadFAQs();
      this.loadArticles();
      return;
    }

    this.helpCenterService.search({ query: this.searchQuery }).subscribe({
      next: (result) => {
        this.faqs = result.faqs;
        this.articles = result.articles;
      },
      error: (error) => {
        console.error('Error searching:', error);
      }
    });
  }

  toggleFAQ(id: string): void {
    if (this.expandedFAQs.has(id)) {
      this.expandedFAQs.delete(id);
    } else {
      this.expandedFAQs.add(id);
    }
  }

  voteFAQ(id: string, helpful: boolean): void {
    this.helpCenterService.voteFAQ(id, { helpful }).subscribe({
      next: () => {
        // Optionally show a success message
      },
      error: (error) => {
        console.error('Error voting on FAQ:', error);
      }
    });
  }
}
