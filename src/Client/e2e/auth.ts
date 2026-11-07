import type { test as base } from '@playwright/test';

export const STORAGE_STATE_PATH = 'playwright/.auth/admin.json';

/**
 * Skips the current test when live credentials are missing or global setup saw a 401 (stale admin password
 * on a reused database). Returns the credentials otherwise.
 */
export function skipUnlessLoggedIn(test: Pick<typeof base, 'skip'>): { email: string; password: string } {
  const email = process.env['PLAYWRIGHT_LOGIN_EMAIL'];
  const password = process.env['PLAYWRIGHT_LOGIN_PASSWORD'];
  test.skip(!email || !password, 'Set PLAYWRIGHT_LOGIN_EMAIL and PLAYWRIGHT_LOGIN_PASSWORD (or use ./run-e2e.sh).');
  test.skip(
    process.env['PLAYWRIGHT_LOGIN_STALE'] === '1',
    'Login returned 401: the seeded administrator password differs from PLAYWRIGHT_LOGIN_PASSWORD (stale database).',
  );
  return { email: email ?? '', password: password ?? '' };
}
