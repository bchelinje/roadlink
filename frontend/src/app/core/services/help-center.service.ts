import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import {
  FAQ,
  HelpArticle,
  CreateFAQRequest,
  UpdateFAQRequest,
  CreateArticleRequest,
  UpdateArticleRequest,
  SearchHelpRequest,
  SearchHelpResult,
  VoteFeedbackRequest,
  HelpCenterFilter
} from '@core/models/help-center.models';

@Injectable({
  providedIn: 'root'
})
export class HelpCenterService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/help`;

  // ==================== FAQs ====================

  /**
   * Get all FAQs (public)
   */
  getFAQs(filter?: HelpCenterFilter): Observable<FAQ[]> {
    let params = new HttpParams();
    if (filter?.category) params = params.set('category', filter.category);
    if (filter?.isPublished !== undefined) params = params.set('isPublished', filter.isPublished.toString());
    if (filter?.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter?.pageSize) params = params.set('pageSize', filter.pageSize.toString());

    return this.http.get<FAQ[]>(`${this.baseUrl}/faqs`, { params });
  }

  /**
   * Get a specific FAQ by ID (public)
   */
  getFAQ(faqId: string): Observable<FAQ> {
    return this.http.get<FAQ>(`${this.baseUrl}/faqs/${faqId}`);
  }

  /**
   * Create FAQ (Admin only)
   */
  createFAQ(request: CreateFAQRequest): Observable<FAQ> {
    return this.http.post<FAQ>(`${this.baseUrl}/faqs`, request);
  }

  /**
   * Update FAQ (Admin only)
   */
  updateFAQ(faqId: string, request: UpdateFAQRequest): Observable<FAQ> {
    return this.http.put<FAQ>(`${this.baseUrl}/faqs/${faqId}`, request);
  }

  /**
   * Delete FAQ (Admin only)
   */
  deleteFAQ(faqId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/faqs/${faqId}`);
  }

  /**
   * Vote on FAQ helpfulness (public)
   */
  voteFAQ(faqId: string, request: VoteFeedbackRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/faqs/${faqId}/vote`, request);
  }

  // ==================== Articles ====================

  /**
   * Get all articles (public)
   */
  getArticles(filter?: HelpCenterFilter): Observable<HelpArticle[]> {
    let params = new HttpParams();
    if (filter?.category) params = params.set('category', filter.category);
    if (filter?.isPublished !== undefined) params = params.set('isPublished', filter.isPublished.toString());
    if (filter?.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter?.pageSize) params = params.set('pageSize', filter.pageSize.toString());

    return this.http.get<HelpArticle[]>(`${this.baseUrl}/articles`, { params });
  }

  /**
   * Get article by slug (public)
   */
  getArticleBySlug(slug: string): Observable<HelpArticle> {
    return this.http.get<HelpArticle>(`${this.baseUrl}/articles/slug/${slug}`);
  }

  /**
   * Get article by ID (public)
   */
  getArticle(articleId: string): Observable<HelpArticle> {
    return this.http.get<HelpArticle>(`${this.baseUrl}/articles/${articleId}`);
  }

  /**
   * Create article (Admin only)
   */
  createArticle(request: CreateArticleRequest): Observable<HelpArticle> {
    return this.http.post<HelpArticle>(`${this.baseUrl}/articles`, request);
  }

  /**
   * Update article (Admin only)
   */
  updateArticle(articleId: string, request: UpdateArticleRequest): Observable<HelpArticle> {
    return this.http.put<HelpArticle>(`${this.baseUrl}/articles/${articleId}`, request);
  }

  /**
   * Delete article (Admin only)
   */
  deleteArticle(articleId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/articles/${articleId}`);
  }

  /**
   * Vote on article helpfulness (public)
   */
  voteArticle(articleId: string, request: VoteFeedbackRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/articles/${articleId}/vote`, request);
  }

  // ==================== Search ====================

  /**
   * Search help content (public)
   */
  search(request: SearchHelpRequest): Observable<SearchHelpResult> {
    let params = new HttpParams()
      .set('query', request.query);

    if (request.category) params = params.set('category', request.category);
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber.toString());
    if (request.pageSize) params = params.set('pageSize', request.pageSize.toString());

    return this.http.get<SearchHelpResult>(`${this.baseUrl}/search`, { params });
  }
}
