export interface Complaint {
  id: string;
  complaintNumber: string;
  complainantId: string;
  complainantName: string;
  complainantEmail: string;
  complainantType: 'Customer' | 'Driver';
  againstId?: string;
  againstName?: string;
  againstType?: 'Customer' | 'Driver';
  relatedJobId?: string;
  category: ComplaintCategory;
  severity: ComplaintSeverity;
  subject: string;
  description: string;
  evidenceUrls: string[];
  witnessInfo?: string;
  status: ComplaintStatus;
  investigationNotes?: InvestigationNote[];
  resolution?: ComplaintResolution;
  escalatedAt?: Date;
  escalationReason?: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface InvestigationNote {
  id: string;
  complaintId: string;
  adminId: string;
  adminName: string;
  note: string;
  isInternal: boolean;
  createdAt: Date;
}

export interface ComplaintResolution {
  resolvedBy: string;
  resolvedByName: string;
  resolutionSummary: string;
  actionsTaken: string[];
  compensationOffered?: string;
  resolvedAt: Date;
}

export interface CreateComplaintRequest {
  againstId?: string;
  againstType?: 'Customer' | 'Driver';
  relatedJobId?: string;
  category: ComplaintCategory;
  severity: ComplaintSeverity;
  subject: string;
  description: string;
  evidenceUrls?: string[];
  witnessInfo?: string;
}

export interface ResolveComplaintRequest {
  resolutionSummary: string;
  actionsTaken: string[];
  compensationOffered?: string;
}

export interface EscalateComplaintRequest {
  reason: string;
}

export interface AddInvestigationNoteRequest {
  note: string;
  isInternal: boolean;
}

export interface ComplaintStatistics {
  totalComplaints: number;
  pendingComplaints: number;
  underInvestigationComplaints: number;
  resolvedComplaints: number;
  escalatedComplaints: number;
  complaintsByCategory: { [key: string]: number };
  complaintsBySeverity: { [key: string]: number };
  averageResolutionTime: number;
}

export enum ComplaintCategory {
  Service = 'service',
  Billing = 'billing',
  Safety = 'safety',
  Conduct = 'conduct',
  Damage = 'damage',
  Other = 'other'
}

export enum ComplaintSeverity {
  Low = 'low',
  Medium = 'medium',
  High = 'high',
  Critical = 'critical'
}

export enum ComplaintStatus {
  Pending = 'pending',
  UnderInvestigation = 'under_investigation',
  Resolved = 'resolved',
  Escalated = 'escalated'
}

export interface ComplaintFilter {
  status?: ComplaintStatus;
  severity?: ComplaintSeverity;
  category?: ComplaintCategory;
  complainantId?: string;
  againstId?: string;
  pageNumber?: number;
  pageSize?: number;
}
