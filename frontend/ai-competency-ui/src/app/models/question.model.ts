export type QuestionType = 'single_choice' | 'multi_choice' | 'mcq_single' | 'mcq_multi' | 'true_false' | 'short_answer' | 'scenario';
export type QuestionStatus = 'draft' | 'reviewed' | 'published' | 'archived';

export interface Question {
  id: string;
  title: string;
  prompt: string;
  type: QuestionType;
  competencyGroupId: string;
  groupCode?: string | null;
  targetLevel?: 1 | 2 | 3 | null;
  competencyLevelId: string;
  difficulty: 1 | 2 | 3 | 4 | 5 | null;
  skillId?: number | null;
  tags: string[];
  options?: string[];
  answer?: any;
  context?: string;
  responseStructure?: string[];
  rubric?: string[];
  judgePromptHints?: string;
  behaviorsAddressed?: string[];
  status: QuestionStatus;
  reviewedBy?: string | null;
  reviewedAt?: string | null;
  reviewNote?: string | null;
  /** Reviewed, published and objective: usable in a test form. */
  eligible?: boolean;
  updatedAt: Date;
}

export interface CoverageCell {
  groupCode: string;
  groupName: string;
  level: 1 | 2 | 3;
  total: number;
  draft: number;
  reviewed: number;
  published: number;
  archived: number;
  eligible: number;
  byType: Record<string, number>;
}

export interface BankCoverage {
  standardMinPerGroup: number;
  standardMaxPerGroup: number;
  unclassified: number;
  cells: CoverageCell[];
}

export interface CompetencyGroup {
  id: string;
  name: string;
  description?: string;
}

export interface CompetencyLevel {
  id: string;
  name: string;
  level: 1 | 2 | 3; // 1: Basic, 2: Intermediate, 3: Advanced
}
