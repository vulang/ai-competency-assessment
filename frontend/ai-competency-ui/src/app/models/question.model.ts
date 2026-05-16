export type QuestionType = 'single_choice' | 'multi_choice' | 'mcq_single' | 'mcq_multi' | 'true_false' | 'short_answer' | 'scenario';
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
  options?: string[];
  answer?: any;
  context?: string;
  responseStructure?: string[];
  rubric?: string[];
  judgePromptHints?: string;
  behaviorsAddressed?: string[];
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
