import { expect, test, type Locator } from '@playwright/test';
import { saveScreenshotsAfterEach } from '../screenshots';

saveScreenshotsAfterEach();

test('review unit price keeps the Romanian decimal comma and thousands dot', async ({ page }) => {
  await page.route('**/api/**', async route => {
    const { pathname } = new URL(route.request().url());
    if (pathname === '/api/Users/manage/info') {
      await route.fulfill({ json: { email: 'test@example.com', isEmailConfirmed: true } });
    } else if (pathname === '/api/SchoolClasses/class-1') {
      await route.fulfill({ json: { id: 'class-1', name: 'Clasa E2E', startDate: '2026-09-01', endDate: '2027-06-15' } });
    } else if (pathname === '/api/SchoolClasses/class-1/summary') {
      await route.fulfill({ json: { id: 'class-1' } });
    } else if (pathname === '/api/GoodsReceipts/imports/get-all') {
      await route.fulfill({ json: {
        data: [{ id: 'import-1', status: 'pendingReview', classId: 'class-1', className: 'Clasa E2E',
          fileMetadataId: 'file-1', blobPath: 'a.pdf', uploadedByName: 'Test user',
          uploadedAt: '2026-10-14T08:00:00Z', createdDate: '2026-10-14T08:00:00Z' }],
        hasNextPage: false, sort: []
      } });
    } else if (pathname === '/api/GoodsReceipts/imports/import-1') {
      await route.fulfill({ json: { id: 'import-1', status: 'pendingReview', classId: 'class-1',
        className: 'Clasa E2E', lines: [{ rawItemText: 'Caiet', name: 'Caiet', unit: 'buc', quantity: 2,
          unitPrice: 3.5, isPerishable: false }] } });
    } else {
      await route.fulfill({ json: { data: [], hasNextPage: false, sort: [], items: [] } });
    }
  });

  await page.goto('/school-classes/class-1?tab=3');
  await page.getByRole('button', { name: 'Revizuieste importul' }).filter({ visible: true }).first().click();

  const price = page.locator('nz-input-number').filter({ hasText: 'RON' }).locator('input').filter({ visible: true }).last();
  await expect(price).toHaveValue('3,50');

  // Real keystrokes: nz-input-number re-formats after each one, which used to make "16,3" untypable.
  await price.click();
  await price.press('ControlOrMeta+a');
  await typeKeys(price, '16,3');
  await expect(price).toHaveValue('16,3');
  await price.blur();
  await expect(price).toHaveValue('16,3');

  await price.click();
  await price.press('ControlOrMeta+a');
  await typeKeys(price, '1.234,50');
  await expect(price).toHaveValue('1.234,50');
});

// One key at a time and awaited: pressSequentially is fast enough to hide the per-keystroke re-formatting.
async function typeKeys(input: Locator, text: string) {
  for (const char of text) await input.press(char === ',' ? 'Comma' : char);
}
