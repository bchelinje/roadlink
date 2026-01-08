// features/activity-logs/activity-log-detail-modal/activity-log-detail-modal.component.ts
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivityLog } from '@core/api';

@Component({
  selector: 'app-activity-log-detail-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './activity-log-detail-modal.component.html',
  styleUrls: ['./activity-log-detail-modal.component.scss']
})
export class ActivityLogDetailModalComponent {
  @Input() log: ActivityLog | null = null;
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();

  onClose(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }

  getMetadata(): any {
    if (!this.log?.metadata) return null;

    try {
      if (typeof this.log.metadata === 'string') {
        return JSON.parse(this.log.metadata);
      }
      return this.log.metadata;
    } catch {
      return null;
    }
  }

  getMetadataKeys(): string[] {
    const metadata = this.getMetadata();
    return metadata ? Object.keys(metadata) : [];
  }

  getMetadataValue(key: string): string {
    const metadata = this.getMetadata();
    if (!metadata) return '';

    const value = metadata[key];
    if (typeof value === 'object') {
      return JSON.stringify(value, null, 2);
    }
    return String(value);
  }

  getSeverityColor(severity: string | null | undefined): string {
    const colorMap: Record<string, string> = {
      'INFO': 'blue',
      'WARNING': 'yellow',
      'ERROR': 'red',
      'CRITICAL': 'purple'
    };
    return colorMap[severity ?? 'INFO'] || 'gray';
  }

  getUserInitials(name: string | null | undefined): string {
    if (!name) return 'U';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }

  getUserAvatarColor(name: string | null | undefined): string {
    if (!name) return 'bg-gray-500';

    const colors = [
      'bg-blue-500',
      'bg-green-500',
      'bg-yellow-500',
      'bg-red-500',
      'bg-purple-500',
      'bg-pink-500',
      'bg-indigo-500',
      'bg-teal-500'
    ];
    const index = name.length % colors.length;
    return colors[index];
  }

  formatDate(date: string | Date | null | undefined): string {
    if (!date) return 'Unknown';

    const d = new Date(date);
    if (isNaN(d.getTime())) return 'Invalid Date';

    return d.toLocaleString('en-GB', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }

  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      // Could add a toast notification here
      console.log('Copied to clipboard');
    });
  }

  copyMetadata(): void {
    const metadata = this.getMetadata();
    if (metadata) {
      this.copyToClipboard(JSON.stringify(metadata, null, 2));
    }
  }

  copyLogDetails(): void {
    if (!this.log) return;

    const details = `
Activity Log Details
====================
ID: ${this.log.id}
Action: ${this.log.action}
Entity Type: ${this.log.entityType}
Entity ID: ${this.log.entityId || 'N/A'}
Entity Name: ${this.log.entityName || 'N/A'}
Description: ${this.log.description}
Severity: ${this.log.severity}
User: ${this.log.userName} (${this.log.userEmail})
IP Address: ${this.log.ipAddress || 'N/A'}
Timestamp: ${this.formatDate(this.log.timestamp)}

Metadata:
${JSON.stringify(this.getMetadata(), null, 2)}
    `.trim();

    this.copyToClipboard(details);
  }
}
