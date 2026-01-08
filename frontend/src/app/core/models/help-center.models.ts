export interface FAQ {
  id: string;
  question: string;
  answer: string;
  category: string;
  tags: string[];
  order: number;
  viewCount: number;
  helpfulCount: number;
  notHelpfulCount: number;
  isPublished: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface HelpArticle {
  id: string;
  title: string;
  slug: string;
  content: string;
  excerpt: string;
  category: string;
  tags: string[];
  coverImageUrl?: string;
  order: number;
  viewCount: number;
  helpfulCount: number;
  notHelpfulCount: number;
  estimatedReadTime: number;
  isPublished: boolean;
  publishedAt?: Date;
  createdAt: Date;
  updatedAt: Date;
}

export interface HelpCategory {
  name: string;
  description?: string;
  icon?: string;
  order: number;
}

export interface CreateFAQRequest {
  question: string;
  answer: string;
  category: string;
  tags?: string[];
  order?: number;
  isPublished?: boolean;
}

export interface UpdateFAQRequest {
  question?: string;
  answer?: string;
  category?: string;
  tags?: string[];
  order?: number;
  isPublished?: boolean;
}

export interface CreateArticleRequest {
  title: string;
  content: string;
  excerpt?: string;
  category: string;
  tags?: string[];
  coverImageUrl?: string;
  order?: number;
  isPublished?: boolean;
}

export interface UpdateArticleRequest {
  title?: string;
  content?: string;
  excerpt?: string;
  category?: string;
  tags?: string[];
  coverImageUrl?: string;
  order?: number;
  isPublished?: boolean;
}

export interface SearchHelpRequest {
  query: string;
  category?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface SearchHelpResult {
  faqs: FAQ[];
  articles: HelpArticle[];
  totalResults: number;
}

export interface VoteFeedbackRequest {
  helpful: boolean;
}

export interface HelpCenterFilter {
  category?: string;
  isPublished?: boolean;
  pageNumber?: number;
  pageSize?: number;
}
