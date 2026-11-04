/** Saves a blob through a temporary object URL. */
export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

/** Reads a readable message from an API error body. */
export function apiError(error: { error?: unknown; status?: number }, fallback: string): string {
  if (typeof error.error === 'string' && error.error.trim()) return error.error;
  const body = error.error as { title?: string; errors?: Record<string, string[]> } | null;
  const details = body?.errors ? Object.values(body.errors).flat().join(' ') : '';
  return details || body?.title || (error.status === 503 ? 'The analysis service is unavailable.' : fallback);
}
