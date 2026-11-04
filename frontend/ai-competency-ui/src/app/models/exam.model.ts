import { GroupScore, LevelNumber } from './test.model';

export interface ReportSummary {
  examId: number | null;
  exams: { examId: number; title: string; isStandard: boolean; attempts: number }[];
  attempts: number;
  candidates: number;
  passRate: number | null;
  averagePercent: number | null;
  scoreDistribution: { from: number; to: number; count: number }[];
  groups: {
    groupCode: string;
    groupName: string;
    sessions: number;
    averagePercent: number | null;
    basic: number;
    intermediate: number;
    advanced: number;
    unmapped: number;
  }[];
}

export interface AttemptRow {
  sessionId: string;
  candidate: string;
  examId: number;
  examTitle: string;
  startTime: string;
  endTime: string | null;
  status: string;
  score: number;
  maxScore: number;
  percent: number | null;
  passScore: number;
  passed: boolean | null;
}

export interface AttemptDetail extends Omit<AttemptRow, 'examId' | 'passScore'> {
  groupScores: GroupScore[];
  responses: {
    questionId: number;
    orderIndex: number;
    title: string;
    type: string;
    groupCode: string | null;
    targetLevel: LevelNumber | null;
    answer: string | null;
    scoreEarned: number;
    pointValue: number;
    feedback: string | null;
  }[];
}
