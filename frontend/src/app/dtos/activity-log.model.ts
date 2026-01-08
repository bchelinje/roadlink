// models/activity-log.model.ts
export interface ActivityLog {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  action: ActivityAction;
  entityType: EntityType;
  entityId?: string;
  entityName?: string;
  description: string;
  ipAddress?: string;
  userAgent?: string;
  metadata?: Record<string, any>;
  timestamp: Date;
  severity: LogSeverity;
}

export enum ActivityAction {
  // User actions
  USER_CREATED = 'USER_CREATED',
  USER_UPDATED = 'USER_UPDATED',
  USER_DELETED = 'USER_DELETED',
  USER_LOCKED = 'USER_LOCKED',
  USER_UNLOCKED = 'USER_UNLOCKED',
  USER_PASSWORD_CHANGED = 'USER_PASSWORD_CHANGED',
  USER_EMAIL_VERIFIED = 'USER_EMAIL_VERIFIED',

  // Authentication actions
  LOGIN_SUCCESS = 'LOGIN_SUCCESS',
  LOGIN_FAILED = 'LOGIN_FAILED',
  LOGOUT = 'LOGOUT',
  PASSWORD_RESET_REQUESTED = 'PASSWORD_RESET_REQUESTED',
  PASSWORD_RESET_COMPLETED = 'PASSWORD_RESET_COMPLETED',

  // Role actions
  ROLE_CREATED = 'ROLE_CREATED',
  ROLE_UPDATED = 'ROLE_UPDATED',
  ROLE_DELETED = 'ROLE_DELETED',
  ROLE_ASSIGNED = 'ROLE_ASSIGNED',
  ROLE_REMOVED = 'ROLE_REMOVED',

  // Move actions (BeC Van Moving specific)
  MOVE_CREATED = 'MOVE_CREATED',
  MOVE_UPDATED = 'MOVE_UPDATED',
  MOVE_CANCELLED = 'MOVE_CANCELLED',
  MOVE_COMPLETED = 'MOVE_COMPLETED',

  // Driver actions
  DRIVER_ASSIGNED = 'DRIVER_ASSIGNED',
  DRIVER_UNASSIGNED = 'DRIVER_UNASSIGNED',
  DRIVER_STATUS_CHANGED = 'DRIVER_STATUS_CHANGED',

  // System actions
  SETTINGS_CHANGED = 'SETTINGS_CHANGED',
  DATA_EXPORTED = 'DATA_EXPORTED',
  DATA_IMPORTED = 'DATA_IMPORTED',
  BACKUP_CREATED = 'BACKUP_CREATED'
}

export enum EntityType {
  USER = 'USER',
  ROLE = 'ROLE',
  MOVE = 'MOVE',
  DRIVER = 'DRIVER',
  VEHICLE = 'VEHICLE',
  SETTINGS = 'SETTINGS',
  SYSTEM = 'SYSTEM'
}

export enum LogSeverity {
  INFO = 'INFO',
  WARNING = 'WARNING',
  ERROR = 'ERROR',
  CRITICAL = 'CRITICAL'
}

export interface ActivityLogFilter {
  userId?: string;
  action?: ActivityAction;
  entityType?: EntityType;
  severity?: LogSeverity;
  startDate?: Date;
  endDate?: Date;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
}

export interface ActivityLogResponse {
  logs: ActivityLog[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
