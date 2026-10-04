import { defineConfig, devices } from '@playwright/test';

// The SPA needs the Web API/SQL/Redis behind its proxy, so start the full stack first:
//   dotnet run --project src/AppHost
// Override the target with PLAYWRIGHT_BASE_URL when needed.
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: process.env['CI'] ? 1 : undefined,
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
