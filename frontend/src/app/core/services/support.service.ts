import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import {
  SupportTicket,
  CreateTicketRequest,
  UpdateTicketRequest,
  AddTicketMessageRequest,
  TicketStatistics,
  TicketFilter
} from '@core/models/support.models';

@Injectable({
  providedIn: 'root'
})
export class SupportService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/support`;

  /**
   * Create a new support ticket
   */
  createTicket(request: CreateTicketRequest): Observable<SupportTicket> {
    return this.http.post<SupportTicket>(`${this.baseUrl}/tickets`, request);
  }

  /**
   * Get tickets list with filtering
   */
  getTickets(filter?: TicketFilter): Observable<SupportTicket[]> {
    let params = new HttpParams();
    if (filter?.status) params = params.set('status', filter.status);
    if (filter?.priority) params = params.set('priority', filter.priority);
    if (filter?.category) params = params.set('category', filter.category);
    if (filter?.assignedToId) params = params.set('assignedToId', filter.assignedToId);
    if (filter?.customerId) params = params.set('customerId', filter.customerId);
    if (filter?.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter?.pageSize) params = params.set('pageSize', filter.pageSize.toString());

    return this.http.get<SupportTicket[]>(`${this.baseUrl}/tickets`, { params });
  }

  /**
   * Get a specific ticket by ID
   */
  getTicket(ticketId: string): Observable<SupportTicket> {
    return this.http.get<SupportTicket>(`${this.baseUrl}/tickets/${ticketId}`);
  }

  /**
   * Update ticket
   */
  updateTicket(ticketId: string, request: UpdateTicketRequest): Observable<SupportTicket> {
    return this.http.put<SupportTicket>(`${this.baseUrl}/tickets/${ticketId}`, request);
  }

  /**
   * Add message to ticket
   */
  addMessage(ticketId: string, request: AddTicketMessageRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tickets/${ticketId}/messages`, request);
  }

  /**
   * Close ticket
   */
  closeTicket(ticketId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tickets/${ticketId}/close`, {});
  }

  /**
   * Rate ticket (customer satisfaction)
   */
  rateTicket(ticketId: string, rating: number, comment?: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tickets/${ticketId}/rate`, { rating, comment });
  }

  /**
   * Get ticket statistics (Admin only)
   */
  getStatistics(): Observable<TicketStatistics> {
    return this.http.get<TicketStatistics>(`${this.baseUrl}/tickets/statistics`);
  }

  /**
   * Assign ticket to agent (Admin only)
   */
  assignTicket(ticketId: string, agentId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tickets/${ticketId}/assign`, { assignedToId: agentId });
  }
}
