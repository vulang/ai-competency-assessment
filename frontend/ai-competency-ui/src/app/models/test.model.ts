export interface TestBlueprint {
  id: string;
  name: string;
  description: string;
  durationMinutes: number;
  passingScore: number;
  questionCount: number;
  status: 'draft' | 'active' | 'archived';
  createdAt: Date;
  updatedAt: Date;
}

export interface TestGenerationConfig {
  blueprintId: string;
  randomize: boolean;
  perCompetencyQuotas: Record<string, number>; // groupId -> count
}
