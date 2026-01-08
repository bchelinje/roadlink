export interface SupportTicket {
  id: string;
  ticketNumber: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  subject: string;
  description: string;
  category: TicketCategory;
  priority: TicketPriority;
  status: TicketStatus;
  assignedToId?: string;
  assignedToName?: string;
  relatedJobId?: string;
  firstResponseAt?: Date;
  resolvedAt?: Date;
  closedAt?: Date;
  satisfactionRating?: number;
  satisfactionComment?: string;
  createdAt: Date;
  updatedAt: Date;
  messages?: TicketMessage[];
}

export interface TicketMessage {
  id: string;
  ticketId: string;
  senderId: string;
  senderName: string;
  senderType: 'Customer' | 'Admin';
  message: string;
  isInternal: boolean;
  attachments?: string[];
  createdAt: Date;
}

export interface CreateTicketRequest {
  subject: string;
  description: string;
  category: TicketCategory;
  priority: TicketPriority;
  relatedJobId?: string;
}

export interface UpdateTicketRequest {
  status?: TicketStatus;
  priority?: TicketPriority;
  assignedToId?: string;
}

export interface AddTicketMessageRequest {
  message: string;
  isInternal?: boolean;
  attachments?: string[];
}

export interface TicketStatistics {
  totalTickets: number;
  openTickets: number;
  inProgressTickets: number;
  resolvedTickets: number;
  closedTickets: number;
  averageFirstResponseTime: number;
  averageResolutionTime: number;
  averageSatisfactionRating: number;
  ticketsByCategory: { [key: string]: number };
  ticketsByPriority: { [key: string]: number };
}

export enum TicketCategory {
  General = 'general',
  Billing = 'billing',
  Technical = 'technical',
  JobIssue = 'job_issue',
  DriverIssue = 'driver_issue',
  Account = 'account',
  Other = 'other'
}

export enum TicketPriority {
  Low = 'low',
  Medium = 'medium',
  High = 'high',
  Urgent = 'urgent'
}

export enum TicketStatus {
  Open = 'open',
  InProgress = 'in_progress',
  Resolved = 'resolved',
  Closed = 'closed'
}

export interface TicketFilter {
  status?: TicketStatus;
  priority?: TicketPriority;
  category?: TicketCategory;
  assignedToId?: string;
  customerId?: string;
  pageNumber?: number;
  pageSize?: number;
}
