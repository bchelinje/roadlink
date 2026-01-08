export interface DashboardOverview {
  userStatistics: UserStatistics;
  jobStatistics: JobStatistics;
  revenueStatistics: RevenueStatistics;
  supportStatistics: SupportStatistics;
  platformHealth: PlatformHealth;
  recentActivity: RecentActivity[];
}

export interface UserStatistics {
  totalUsers: number;
  activeCustomers: number;
  activeDrivers: number;
  newUsersToday: number;
  newUsersThisWeek: number;
  newUsersThisMonth: number;
  userGrowthRate: number;
  customerRetentionRate: number;
}

export interface JobStatistics {
  totalJobs: number;
  pendingJobs: number;
  inProgressJobs: number;
  completedJobs: number;
  cancelledJobs: number;
  jobsToday: number;
  jobsThisWeek: number;
  jobsThisMonth: number;
  averageJobValue: number;
  jobCompletionRate: number;
}

export interface RevenueStatistics {
  totalRevenue: number;
  revenueToday: number;
  revenueThisWeek: number;
  revenueThisMonth: number;
  revenueThisYear: number;
  averageRevenuePerJob: number;
  revenueGrowthRate: number;
  projectedMonthlyRevenue: number;
}

export interface SupportStatistics {
  openTickets: number;
  activeComplaints: number;
  averageResponseTime: number;
  customerSatisfactionScore: number;
}

export interface PlatformHealth {
  systemStatus: 'healthy' | 'degraded' | 'down';
  activeDriversOnline: number;
  averageJobAssignmentTime: number;
  apiResponseTime: number;
  errorRate: number;
}

export interface RecentActivity {
  id: string;
  type: 'user_registration' | 'job_created' | 'job_completed' | 'payment_received' | 'ticket_created' | 'complaint_filed';
  description: string;
  timestamp: Date;
  userId?: string;
  userName?: string;
}

export interface UserAnalytics {
  period: 'daily' | 'weekly' | 'monthly';
  data: UserAnalyticsDataPoint[];
}

export interface UserAnalyticsDataPoint {
  date: string;
  newUsers: number;
  activeUsers: number;
  churnedUsers: number;
}

export interface JobAnalytics {
  period: 'daily' | 'weekly' | 'monthly';
  data: JobAnalyticsDataPoint[];
}

export interface JobAnalyticsDataPoint {
  date: string;
  totalJobs: number;
  completedJobs: number;
  cancelledJobs: number;
  averageValue: number;
}

export interface RevenueAnalytics {
  period: 'daily' | 'weekly' | 'monthly';
  data: RevenueAnalyticsDataPoint[];
}

export interface RevenueAnalyticsDataPoint {
  date: string;
  revenue: number;
  jobCount: number;
  averageJobValue: number;
}
