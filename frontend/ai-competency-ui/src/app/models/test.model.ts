export type TestStatus = 'draft' | 'active' | 'archived';
export type LevelNumber = 1 | 2 | 3;

/** Stored blueprint; levelMix holds target percentages per level and is empty for any level. */
export interface TestBlueprintSpec {
  groupQuotas: Record<string, number>;
  levelMix: Partial<Record<LevelNumber, number>>;
  seed: number;
  passPercent: number;
}

export interface TestBlueprint {
  id: string;
  name: string;
  description: string;
  durationMinutes: number;
  /** Raw points needed to pass; fixed when the test is published. */
  passingScore: number;
  questionCount: number;
  status: TestStatus;
  isStandard: boolean;
  blueprint: TestBlueprintSpec | null;
  createdAt: string;
  updatedAt: string;
  publishedAt: string | null;
}

export interface TestQuestion {
  questionId: number;
  orderIndex: number;
  pointValue: number;
  title: string;
  type: string;
  groupCode: string | null;
  targetLevel: LevelNumber | null;
  difficulty: number | null;
  status: string;
  eligible: boolean;
}

export interface GroupCoverage {
  groupCode: string;
  groupName: string;
  quota: number;
  total: number;
  basic: number;
  intermediate: number;
  advanced: number;
}

export interface TestDetail extends TestBlueprint {
  questions: TestQuestion[];
  coverage: GroupCoverage[];
  standardIssues: string[];
  hasSessions: boolean;
}

export interface SaveTestRequest {
  name: string;
  description: string;
  durationMinutes: number;
  passPercent: number;
  isStandard: boolean;
  groupQuotas: Record<string, number>;
  levelMix: Partial<Record<LevelNumber, number>>;
  seed: number | null;
}

export interface TestPreview {
  seed: number;
  questions: TestQuestion[];
  coverage: GroupCoverage[];
  warnings: string[];
  standardIssues: string[];
}

export interface GroupScore {
  groupCode: string;
  groupName: string;
  order: number;
  itemCount: number;
  rawScore: number | null;
  maxScore: number | null;
  percent: number | null;
  level: LevelNumber | null;
  levelCode: 'BASIC' | 'INTERMEDIATE' | 'ADVANCED' | null;
  levelMappingVersion: string | null;
}

export interface TestProfile {
  sessionId: string;
  examTitle: string;
  completedAt: string;
  groups: GroupScore[];
}

export interface LevelMapping {
  id: number;
  version: string;
  status: 'Draft' | 'Published';
  isActive: boolean;
  intermediateMinPercent: number;
  advancedMinPercent: number;
  minItemsPerGroup: number;
  source: string;
  reviewedBy: string | null;
  reviewNotes: string | null;
  publishedAt: string | null;
  createdAt: string;
}

// ── CTT item analysis (thesis §2.12, Appendix H) ───────────────────────────

export interface CttDistractor {
  option: number;
  isKey: boolean;
  proportion: number;
  upper: number | null;
  lower: number | null;
  optionRestR: number | null;
  flags: string[];
}

export interface CttItemResult {
  itemId: string;
  group: string | null;
  n: number;
  p: number | null;
  itemRestR: number | null;
  itemRestRGroup: number | null;
  discriminationIndex: number | null;
  alphaIfDeleted: number | null;
  omitRate: number | null;
  flags: string[];
  distractors: CttDistractor[];
}

export interface CttReliability {
  nItems: number;
  alpha: number | null;
  omega: number | null;
  excludedItems: string[];
  notes: string[];
}

export interface CttGroupResult extends CttReliability {
  group: string;
  meanPercent: number | null;
}

export interface CttAnalysis {
  nRespondents: number;
  nItems: number;
  total: { mean: number | null; sd: number | null; min: number | null; max: number | null; histogram: { score: number; count: number }[] };
  reliability: CttReliability;
  groups: CttGroupResult[];
  items: CttItemResult[];
  thresholds: { minDifficulty: number; maxDifficulty: number; minDiscrimination: number; minDistractorRate: number };
}

export interface ItemAnalysisItem {
  questionId: number;
  orderIndex: number;
  title: string;
  type: string;
  groupCode: string | null;
  targetLevel: LevelNumber | null;
  options: string[];
}

export interface ItemAnalysisReport {
  examId: number;
  examTitle: string;
  examStatus: TestStatus;
  generatedAt: string;
  items: ItemAnalysisItem[];
  excludedNonObjective: number;
  analysis: CttAnalysis;
}

export type ItemAnalysisExport = 'items' | 'reliability' | 'responses';
