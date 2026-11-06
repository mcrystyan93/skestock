import { existsSync } from 'node:fs';
import { expect, test } from '@playwright/test';
import { skipUnlessLoggedIn, STORAGE_STATE_PATH } from '../auth';

// The file is absent without credentials (or on a stale login); the test then skips instead of failing to open a context.
// Real backend, no page.route: reuses the session saved by global setup and reads the e2e demo seed.
test.use({ ignoreHTTPSErrors: true, storageState: existsSync(STORAGE_STATE_PATH) ? STORAGE_STATE_PATH : undefined });

test('the item list shows the seeded demo items', async ({ page }) => {
  skipUnlessLoggedIn(test);

  await page.goto('/items');
  await page.getByPlaceholder('Cautați...').fill('DEMO-01');

  // The search narrows the list: exactly one DEMO-01 entry, and another seeded item is gone.
  await expect(page.getByText(/PUR-\d\d/)).toHaveCount(0);
  await expect(page.getByText(/DEMO-01\b/)).toHaveCount(1);
  await expect(page.getByText(/DEMO-02\b/)).toHaveCount(0);
});
