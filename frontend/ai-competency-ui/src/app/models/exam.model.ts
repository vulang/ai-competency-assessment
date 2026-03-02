export interface ExamAttempt {
  id: string;
  candidateName: string;
  testName: string;
  score: number;
  passingScore: number;
  status: 'passed' | 'failed';
  completedAt: Date;
  details?: any;
}
