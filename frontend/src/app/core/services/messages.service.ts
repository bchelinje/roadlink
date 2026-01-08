import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { Message, Conversation, SendMessageRequest, ConversationFilter } from '@core/models/message.models';

@Injectable({
  providedIn: 'root'
})
export class MessagesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/messages`;

  /**
   * Send a message
   */
  sendMessage(request: SendMessageRequest): Observable<Message> {
    return this.http.post<Message>(`${this.baseUrl}/send`, request);
  }

  /**
   * Get conversations list
   */
  getConversations(filter?: ConversationFilter): Observable<Conversation[]> {
    let params = new HttpParams();
    if (filter?.jobId) params = params.set('jobId', filter.jobId);
    if (filter?.isArchived !== undefined) params = params.set('isArchived', filter.isArchived.toString());
    if (filter?.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter?.pageSize) params = params.set('pageSize', filter.pageSize.toString());

    return this.http.get<Conversation[]>(`${this.baseUrl}/conversations`, { params });
  }

  /**
   * Get messages for a conversation
   */
  getConversationMessages(conversationId: string, pageNumber = 1, pageSize = 50): Observable<Message[]> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<Message[]>(`${this.baseUrl}/conversations/${conversationId}/messages`, { params });
  }

  /**
   * Get unread message count
   */
  getUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/unread-count`);
  }

  /**
   * Mark message as read
   */
  markAsRead(messageId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${messageId}/read`, {});
  }

  /**
   * Mark all messages in conversation as read
   */
  markConversationAsRead(conversationId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/conversations/${conversationId}/read`, {});
  }

  /**
   * Archive conversation
   */
  archiveConversation(conversationId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/conversations/${conversationId}/archive`, {});
  }
}
