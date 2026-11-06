import { defineConfig, devices } from '@playwright/test';

// The SPA needs the Web API/SQL/Redis behind its proxy. From the repo root, ./run-e2e.sh starts an ephemeral
// stack, runs this suite and tears it down; to target a stack you started yourself, see the README.
// Override the target with PLAYWRIGHT_BASE_URL when needed.
export default defineConfig({
  testDir: './e2e',
  globalSetup: './e2e/global-setup.ts',
  timeout: 60_000,
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: process.env['CI'] ? 1 : '50%',
  reporter: [['html', { open: 'never' }], ['list']],
  use: {
    baseURL: process.env['PLAYWRIGHT_BASE_URL'] ?? 'http://webfrontend-skestock.dev.localhost:7001',
    locale: 'ro-RO',
    timezoneId: 'Europe/Bucharest',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'mobile', use: { ...devices['Pixel 7'] } },
  ],
});
