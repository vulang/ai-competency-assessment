import { LevelNumber, TestBlueprintSpec } from '../../models/test.model';

/** The seven framework groups in thesis order. */
export const COMPETENCY_GROUPS: readonly { code: string; name: string }[] = [
  { code: 'FOUNDATION', name: 'Fundamental' },
  { code: 'DATA', name: 'Data' },
  { code: 'CRITICAL_THINKING', name: 'Critical Thinking' },
  { code: 'APPLICATION', name: 'AI Use Cases' },
  { code: 'ETHICS', name: 'AI Ethics' },
  { code: 'TOOLS', name: 'AI Tools' },
  { code: 'FUTURE_OF_WORK', name: 'Future of Work' }
];

export const LEVEL_NAMES: Record<LevelNumber, string> = { 1: 'Basic', 2: 'Intermediate', 3: 'Advanced' };

// Thesis §2.12: a short form of 35–42 items, about 5–6 per group. Mirrors TestBlueprints on the API.
export const STANDARD_FORM = { minPerGroup: 5, maxPerGroup: 6, minTotal: 35, maxTotal: 42, maxPerGroupAny: 20 } as const;

export function defaultQuotas(perGroup = 6): Record<string, number> {
  return Object.fromEntries(COMPETENCY_GROUPS.map(g => [g.code, perGroup]));
}

export function totalQuota(quotas: Record<string, number>): number {
  return Object.values(quotas).reduce((sum, n) => sum + (Number(n) || 0), 0);
}

/** Lists departures from the thesis short-form specification for the given counts per group. */
export function standardIssues(countsByGroup: Record<string, number>): string[] {
  const issues = COMPETENCY_GROUPS
    .map(g => ({ code: g.code, count: countsByGroup[g.code] ?? 0 }))
    .filter(g => g.count < STANDARD_FORM.minPerGroup || g.count > STANDARD_FORM.maxPerGroup)
    .map(g => `${g.code} has ${g.count} questions; a standard form needs ${STANDARD_FORM.minPerGroup}–${STANDARD_FORM.maxPerGroup}.`);
  const total = totalQuota(countsByGroup);
  if (total < STANDARD_FORM.minTotal || total > STANDARD_FORM.maxTotal)
    issues.push(`The form has ${total} questions; a standard form needs ${STANDARD_FORM.minTotal}–${STANDARD_FORM.maxTotal}.`);
  return issues;
}

/** Returns an error message, or null when the level mix is empty or sums to 100. */
export function levelMixError(mix: Partial<Record<LevelNumber, number>>): string | null {
  const values = Object.values(mix).map(v => Number(v) || 0);
  if (values.some(v => v < 0 || v > 100)) return 'Level percentages must be between 0 and 100.';
  const sum = values.reduce((a, b) => a + b, 0);
  return sum === 0 || sum === 100 ? null : `Level percentages add up to ${sum}; they must add up to 100.`;
}

export function blueprintSummary(spec: TestBlueprintSpec | null): string {
  if (!spec) return 'No blueprint';
  const mix = Object.entries(spec.levelMix ?? {}).filter(([, v]) => v);
  return `${totalQuota(spec.groupQuotas)} items` + (mix.length ? ` · levels ${mix.map(([l, v]) => `${LEVEL_NAMES[+l as LevelNumber][0]} ${v}%`).join(' / ')}` : '') + ` · seed ${spec.seed}`;
}

export function groupName(code: string | null | undefined): string {
  return COMPETENCY_GROUPS.find(g => g.code === code)?.name ?? code ?? '—';
}
