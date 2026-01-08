export interface JobBid {
  id: string;
  jobId: string;
  driverId: string;
  driverName: string;
  driverRating?: number;
  bidAmount: number;
  estimatedDuration: number;
  message?: string;
  status: BidStatus;
  expiresAt: Date;
  createdAt: Date;
  acceptedAt?: Date;
  rejectedAt?: Date;
  withdrawnAt?: Date;
}

export interface CreateBidRequest {
  jobId: string;
  bidAmount: number;
  estimatedDuration: number;
  message?: string;
}

export interface BidFilter {
  jobId?: string;
  driverId?: string;
  status?: BidStatus;
  pageNumber?: number;
  pageSize?: number;
}

export enum BidStatus {
  Pending = 'pending',
  Accepted = 'accepted',
  Rejected = 'rejected',
  Withdrawn = 'withdrawn',
  Expired = 'expired'
}
