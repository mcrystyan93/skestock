import { mkdirSync } from 'node:fs';
import { dirname } from 'node:path';
import { request, type FullConfig } from '@playwright/test';
import { STORAGE_STATE_PATH } from './auth';

// Fails fast with a readable message when the stack is down, then logs in once so specs can reuse the session.
export default async function globalSetup(config: FullConfig): Promise<void> {
  const baseURL = config.projects[0]?.use.baseURL;
  if (!baseURL) throw new Error('Playwright baseURL is not configured.');

  const api = await request.newContext({ baseURL, ignoreHTTPSErrors: true });
  try {
    // 401 is the healthy answer for an anonymous caller; anything unreachable or 5xx means the stack is not ready.
    const info = await api.get('/api/Users/manage/info').catch((error: unknown) => {
      throw new Error(
        `Frontend/API unreachable at ${baseURL}. Start the stack with ./run-e2e.sh (or aspire start). ${String(error)}`,
      );
    });
    if (info.status() >= 500) {
      throw new Error(`API at ${baseURL} is not healthy (/api/Users/manage/info returned ${info.status()}).`);
    }

    const email = process.env['PLAYWRIGHT_LOGIN_EMAIL'];
    const password = process.env['PLAYWRIGHT_LOGIN_PASSWORD'];
    if (!email || !password) return;

    const login = await api.post('/api/Users/login?useCookies=true', { data: { email, password } });
    if (login.status() === 401) {
      process.env['PLAYWRIGHT_LOGIN_STALE'] = '1';
      console.warn('Login returned 401: live specs will be skipped (stale admin password).');
      return;
    }
    if (!login.ok()) throw new Error(`Login failed with status ${login.status()}.`);

    mkdirSync(dirname(STORAGE_STATE_PATH), { recursive: true });
    await api.storageState({ path: STORAGE_STATE_PATH });
  } finally {
    await api.dispose();
  }
}
