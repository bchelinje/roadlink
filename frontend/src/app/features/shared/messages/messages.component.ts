import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MessagesService } from '@core/services/messages.service';
import { Conversation, Message, SendMessageRequest } from '@core/models/message.models';
import { AuthService } from '@core/services/auth.service';
import { interval, Subscription } from 'rxjs';

@Component({
  selector: 'app-messages',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './messages.component.html',
  styleUrls: ['./messages.component.scss']
})
export class MessagesComponent implements OnInit, OnDestroy {
  private messagesService = inject(MessagesService);
  private authService = inject(AuthService);

  conversations: Conversation[] = [];
  selectedConversation: Conversation | null = null;
  messages: Message[] = [];
  newMessage = '';
  isLoading = false;
  isSending = false;
  errorMessage = '';
  unreadCount = 0;

  private refreshSubscription?: Subscription;

  ngOnInit(): void {
    this.loadConversations();
    this.loadUnreadCount();

    // Refresh messages every 10 seconds when a conversation is selected
    this.refreshSubscription = interval(10000).subscribe(() => {
      if (this.selectedConversation) {
        this.loadMessages(this.selectedConversation.id, false);
      }
      this.loadUnreadCount();
    });
  }

  ngOnDestroy(): void {
    this.refreshSubscription?.unsubscribe();
  }

  loadConversations(): void {
    this.isLoading = true;
    this.messagesService.getConversations().subscribe({
      next: (conversations) => {
        this.conversations = conversations;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading conversations:', error);
        this.errorMessage = 'Failed to load conversations';
        this.isLoading = false;
      }
    });
  }

  loadUnreadCount(): void {
    this.messagesService.getUnreadCount().subscribe({
      next: (count) => {
        this.unreadCount = count;
      },
      error: (error) => {
        console.error('Error loading unread count:', error);
      }
    });
  }

  selectConversation(conversation: Conversation): void {
    this.selectedConversation = conversation;
    this.loadMessages(conversation.id);
    this.markConversationAsRead(conversation.id);
  }

  loadMessages(conversationId: string, showLoading = true): void {
    if (showLoading) {
      this.isLoading = true;
    }

    this.messagesService.getConversationMessages(conversationId).subscribe({
      next: (messages) => {
        this.messages = messages.sort((a, b) =>
          new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime()
        );
        this.isLoading = false;

        // Scroll to bottom
        setTimeout(() => this.scrollToBottom(), 100);
      },
      error: (error) => {
        console.error('Error loading messages:', error);
        this.errorMessage = 'Failed to load messages';
        this.isLoading = false;
      }
    });
  }

  sendMessage(): void {
    if (!this.newMessage.trim() || !this.selectedConversation) {
      return;
    }

    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) {
      return;
    }

    // Determine recipient based on current user role
    const isDriver = currentUser.roles?.includes('Driver');
    const recipientId = isDriver
      ? this.selectedConversation.customerId
      : this.selectedConversation.driverId;

    const request: SendMessageRequest = {
      recipientId: recipientId,
      jobId: this.selectedConversation.jobId,
      content: this.newMessage.trim(),
      messageType: 'text'
    };

    this.isSending = true;
    this.messagesService.sendMessage(request).subscribe({
      next: (message) => {
        this.messages.push(message);
        this.newMessage = '';
        this.isSending = false;
        this.scrollToBottom();

        // Update conversation list
        this.loadConversations();
      },
      error: (error) => {
        console.error('Error sending message:', error);
        this.errorMessage = 'Failed to send message';
        this.isSending = false;
      }
    });
  }

  markConversationAsRead(conversationId: string): void {
    this.messagesService.markConversationAsRead(conversationId).subscribe({
      next: () => {
        // Update unread count
        this.loadUnreadCount();

        // Update conversation unread count
        const conv = this.conversations.find(c => c.id === conversationId);
        if (conv) {
          conv.unreadCount = 0;
        }
      },
      error: (error) => {
        console.error('Error marking conversation as read:', error);
      }
    });
  }

  archiveConversation(conversationId: string): void {
    if (!confirm('Are you sure you want to archive this conversation?')) {
      return;
    }

    this.messagesService.archiveConversation(conversationId).subscribe({
      next: () => {
        this.conversations = this.conversations.filter(c => c.id !== conversationId);
        if (this.selectedConversation?.id === conversationId) {
          this.selectedConversation = null;
          this.messages = [];
        }
      },
      error: (error) => {
        console.error('Error archiving conversation:', error);
        this.errorMessage = 'Failed to archive conversation';
      }
    });
  }

  formatTime(date: Date | undefined): string {
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  }

  formatDate(date: Date | undefined): string {
    if (!date) return '';
    const d = new Date(date);
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    if (d.toDateString() === today.toDateString()) {
      return 'Today';
    } else if (d.toDateString() === yesterday.toDateString()) {
      return 'Yesterday';
    } else {
      return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    }
  }

  private scrollToBottom(): void {
    try {
      const messagesContainer = document.querySelector('.messages-container');
      if (messagesContainer) {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
      }
    } catch (err) {
      console.error('Error scrolling to bottom:', err);
    }
  }
}
