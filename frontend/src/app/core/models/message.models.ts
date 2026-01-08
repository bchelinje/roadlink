export interface Message {
  id: string;
  conversationId: string;
  senderId: string;
  senderName: string;
  senderType: 'Customer' | 'Driver' | 'Admin';
  recipientId: string;
  recipientName: string;
  recipientType: 'Customer' | 'Driver' | 'Admin';
  jobId?: string;
  content: string;
  messageType: 'text' | 'image' | 'file' | 'location';
  attachmentUrl?: string;
  attachmentName?: string;
  latitude?: number;
  longitude?: number;
  isRead: boolean;
  readAt?: Date;
  sentAt: Date;
  createdAt: Date;
}

export interface Conversation {
  id: string;
  jobId?: string;
  customerId: string;
  customerName: string;
  driverId: string;
  driverName: string;
  lastMessage?: string;
  lastMessageAt?: Date;
  unreadCount: number;
  isArchived: boolean;
  createdAt: Date;
}

export interface SendMessageRequest {
  recipientId: string;
  jobId?: string;
  content: string;
  messageType: 'text' | 'image' | 'file' | 'location';
  attachmentUrl?: string;
  attachmentName?: string;
  latitude?: number;
  longitude?: number;
}

export interface ConversationFilter {
  jobId?: string;
  isArchived?: boolean;
  pageNumber?: number;
  pageSize?: number;
}
