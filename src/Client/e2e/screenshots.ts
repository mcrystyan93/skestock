import path from 'node:path';
import { test } from '@playwright/test';

// Repo-root screenshot-checks/<spec>/ (git-ignored). The e2e cwd is src/Client, so resolve from this file.
const screenshotRoot = path.resolve(__dirname, '../../../screenshot-checks');

// Call once at the top level of a spec: every test then saves a full-page screenshot of its final state.
export function saveScreenshotsAfterEach(): void {
  test.afterEach(async ({ page }, testInfo) => {
    const spec = path.basename(testInfo.file, '.spec.ts');
    const slug = testInfo.title.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
    const name = `${testInfo.project.name}-${slug}-r${testInfo.repeatEachIndex}-t${testInfo.retry}.png`;
    await page.screenshot({ path: path.join(screenshotRoot, spec, name), fullPage: true });
  });
}

export { screenshotRoot };
