export type QuestionType = 'single_choice' | 'multi_choice' | 'true_false' | 'short_answer' | 'scenario';
export type QuestionStatus = 'draft' | 'published' | 'archived';

export interface Question {
  id: string;
  title: string;
  prompt: string;
  type: QuestionType;
  competencyGroupId: string;
  competencyLevelId: string;
  difficulty: 1 | 2 | 3 | 4 | 5;
  tags: string[];
  status: QuestionStatus;
  updatedAt: Date;
}

export interface CompetencyGroup {
  id: string;
  name: string;
  description?: string;
}

export interface CompetencyLevel {
  id: string;
  name: string;
  level: 1 | 2 | 3; // 1: Foundation, 2: Apply, 3: Create
}
