// features/driver/schedule/schedule.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DriversService } from '@core/api/api/drivers.service';
import { JobDto } from '@core/api/model/models';

interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
  jobs: JobDto[];
}

interface CalendarWeek {
  days: CalendarDay[];
}

@Component({
  selector: 'app-driver-schedule',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './schedule.component.html',
  styleUrls: ['./schedule.component.scss']
})
export class DriverScheduleComponent implements OnInit {
  private readonly driversService = inject(DriversService);

  currentDate = new Date();
  viewMode: 'week' | 'month' = 'week';
  selectedDate: Date | null = null;
  selectedJob: JobDto | null = null;

  jobs: JobDto[] = [];
  loading = true;
  error: string | null = null;

  calendarWeeks: CalendarWeek[] = [];
  weekDays: CalendarDay[] = [];
  monthName = '';
  yearName = '';

  statusFilter: string = 'all';
  statuses = [
    { value: 'all', label: 'All Statuses' },
    { value: 'assigned', label: 'Assigned' },
    { value: 'confirmed', label: 'Confirmed' },
    { value: 'in_progress', label: 'In Progress' },
    { value: 'completed', label: 'Completed' }
  ];

  ngOnInit(): void {
    this.loadJobs();
  }

  private loadJobs(): void {
    this.loading = true;
    this.error = null;

    // Get start and end dates for the current view
    const { startDate, endDate } = this.getDateRange();

    const status = this.statusFilter !== 'all' ? this.statusFilter : undefined;

    this.driversService.apiDriversMeJobsGet(status, startDate.toISOString(), endDate.toISOString(), 1, 1000).subscribe({
      next: (response: any) => {
        this.jobs = response.items || [];
        this.buildCalendar();
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Failed to load jobs:', err);
        this.error = 'Failed to load schedule. Please try again.';
        this.loading = false;
      }
    });
  }

  private getDateRange(): { startDate: Date; endDate: Date } {
    if (this.viewMode === 'week') {
      const startDate = this.getWeekStart(this.currentDate);
      const endDate = new Date(startDate);
      endDate.setDate(endDate.getDate() + 6);
      return { startDate, endDate };
    } else {
      const startDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth(), 1);
      const endDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1, 0);
      return { startDate, endDate };
    }
  }

  private getWeekStart(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day; // Sunday as start of week
    return new Date(d.setDate(diff));
  }

  private buildCalendar(): void {
    if (this.viewMode === 'week') {
      this.buildWeekView();
    } else {
      this.buildMonthView();
    }
    this.updateDisplayNames();
  }

  private buildWeekView(): void {
    const weekStart = this.getWeekStart(this.currentDate);
    this.weekDays = [];

    for (let i = 0; i < 7; i++) {
      const date = new Date(weekStart);
      date.setDate(date.getDate() + i);

      this.weekDays.push({
        date,
        isCurrentMonth: true,
        isToday: this.isToday(date),
        jobs: this.getJobsForDate(date)
      });
    }
  }

  private buildMonthView(): void {
    const year = this.currentDate.getFullYear();
    const month = this.currentDate.getMonth();
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);

    // Get the first Sunday before or on the first day of the month
    const startDate = this.getWeekStart(firstDay);

    // Get the last Saturday after or on the last day of the month
    const endDate = new Date(startDate);
    endDate.setDate(endDate.getDate() + 41); // 6 weeks maximum

    this.calendarWeeks = [];
    let currentDate = new Date(startDate);

    while (currentDate <= endDate) {
      const week: CalendarWeek = { days: [] };

      for (let i = 0; i < 7; i++) {
        const date = new Date(currentDate);
        week.days.push({
          date,
          isCurrentMonth: date.getMonth() === month,
          isToday: this.isToday(date),
          jobs: this.getJobsForDate(date)
        });

        currentDate.setDate(currentDate.getDate() + 1);
      }

      this.calendarWeeks.push(week);

      // Stop after we've covered the entire month
      if (currentDate > lastDay && week.days[6].date > lastDay) {
        break;
      }
    }
  }

  private getJobsForDate(date: Date): JobDto[] {
    return this.jobs.filter(job => {
      if (!job.scheduledDate) return false;
      const jobDate = new Date(job.scheduledDate);
      return (
        jobDate.getFullYear() === date.getFullYear() &&
        jobDate.getMonth() === date.getMonth() &&
        jobDate.getDate() === date.getDate()
      );
    });
  }

  private isToday(date: Date): boolean {
    const today = new Date();
    return (
      date.getFullYear() === today.getFullYear() &&
      date.getMonth() === today.getMonth() &&
      date.getDate() === today.getDate()
    );
  }

  private updateDisplayNames(): void {
    this.monthName = this.currentDate.toLocaleDateString('en-US', { month: 'long' });
    this.yearName = this.currentDate.getFullYear().toString();
  }

  switchView(mode: 'week' | 'month'): void {
    this.viewMode = mode;
    this.buildCalendar();
  }

  previousPeriod(): void {
    if (this.viewMode === 'week') {
      this.currentDate.setDate(this.currentDate.getDate() - 7);
    } else {
      this.currentDate.setMonth(this.currentDate.getMonth() - 1);
    }
    this.currentDate = new Date(this.currentDate);
    this.loadJobs();
  }

  nextPeriod(): void {
    if (this.viewMode === 'week') {
      this.currentDate.setDate(this.currentDate.getDate() + 7);
    } else {
      this.currentDate.setMonth(this.currentDate.getMonth() + 1);
    }
    this.currentDate = new Date(this.currentDate);
    this.loadJobs();
  }

  goToToday(): void {
    this.currentDate = new Date();
    this.loadJobs();
  }

  onStatusFilterChange(): void {
    this.loadJobs();
  }

  selectDate(day: CalendarDay): void {
    this.selectedDate = day.date;
  }

  selectJob(job: JobDto): void {
    this.selectedJob = job;
  }

  closeJobDetails(): void {
    this.selectedJob = null;
  }

  getStatusBadgeClass(status: string): string {
    const statusMap: Record<string, string> = {
      'pending': 'bg-yellow-100 text-yellow-800',
      'assigned': 'bg-blue-100 text-blue-800',
      'confirmed': 'bg-purple-100 text-purple-800',
      'in_progress': 'bg-indigo-100 text-indigo-800',
      'completed': 'bg-green-100 text-green-800',
      'cancelled': 'bg-red-100 text-red-800',
      'on_hold': 'bg-gray-100 text-gray-800'
    };
    return statusMap[status] || 'bg-gray-100 text-gray-800';
  }

  getPriorityBadgeClass(priority: string): string {
    const priorityMap: Record<string, string> = {
      'urgent': 'bg-red-100 text-red-800',
      'high': 'bg-orange-100 text-orange-800',
      'normal': 'bg-blue-100 text-blue-800',
      'low': 'bg-gray-100 text-gray-800'
    };
    return priorityMap[priority] || 'bg-gray-100 text-gray-800';
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  formatTime(time: string | null | undefined): string {
    if (!time) return 'N/A';
    return new Date(time).toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  getDayName(date: Date): string {
    return date.toLocaleDateString('en-US', { weekday: 'short' });
  }

  getDayNumber(date: Date): number {
    return date.getDate();
  }

  getWeekDateRange(): string {
    if (this.weekDays.length === 0) return '';
    const start = this.weekDays[0].date;
    const end = this.weekDays[6].date;
    return `${start.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${end.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
  }
}
