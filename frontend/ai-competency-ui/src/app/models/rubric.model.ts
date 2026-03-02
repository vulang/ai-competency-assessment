export interface RubricCriteria {
  id: string;
  description: string;
  maxScore: number;
}

export interface Rubric {
  id: string;
  name: string;
  competencyGroupId: string;
  competencyLevelId: string;
  criteria: RubricCriteria[];
  updatedAt: Date;
}
