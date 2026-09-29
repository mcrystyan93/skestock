import { expect, test } from '@playwright/test';

test.describe('seed', () => {
  test('opens the login page', async ({ page }) => {
    await page.goto('/login');

    await expect(page.getByRole('heading', { name: 'Loghează-te în contul tău' })).toBeVisible();
  });
});
