import { expect, test, type Page } from '@playwright/test';
import { saveScreenshotsAfterEach, screenshotRoot } from '../screenshots';

saveScreenshotsAfterEach();

const shots = `${screenshotRoot}/item-merge`;

const item = (id: string, name: string, sku: string, isActive = true) => ({
  id,
  name,
  sku,
  description: null,
  unit: 'bucată',
  locationThresholds: [],
  isPerishable: false,
  shelfLifeDays: null,
  isActive,
  categoryId: 'category-1',
  categoryName: 'Papetărie',
  imageFileId: null,
  createdByName: 'Admin',
  lastModifiedByName: 'Admin',
  createdDate: '2026-01-01T00:00:00Z',
  lastModifiedDate: '2026-01-01T00:00:00Z',
});

const items = [item('item-1', 'Creion HB', 'CR-1'), item('item-2', 'Creion HB 12 buc', 'CR-2'), item('item-3', 'Marker negru', 'MK-1')];

const preview = {
  impacts: [
    { kind: 'stockBatches', relinked: 4, combined: 0 },
    { kind: 'supplyListLines', relinked: 1, combined: 2 },
  ],
};

async function mockApi(page: Page, mergeStatus: 204 | 403) {
  const state = { listLoads: 0, mergeBody: null as unknown };

  await page.route('**/api/**', async (route) => {
    const { pathname } = new URL(route.request().url());

    if (pathname === '/api/Users/manage/info') return route.fulfill({ json: { email: 'test@example.com', isEmailConfirmed: true } });
    if (pathname === '/api/Items/get-all') {
      state.listLoads++;
      return route.fulfill({ json: { data: items, hasNextPage: false, nextCursor: null, sort: [] } });
    }
    if (pathname === '/api/Items/merge-preview') return route.fulfill({ json: preview });
    if (pathname === '/api/Items/merge') {
      state.mergeBody = route.request().postDataJSON();
      return mergeStatus === 204
        ? route.fulfill({ status: 204 })
        : route.fulfill({ status: 403, contentType: 'application/problem+json', json: { status: 403, title: 'Forbidden', error: { code: 'auth.forbidden', errors: [] } } });
    }
    return route.fulfill({ json: { data: [], hasNextPage: false, nextCursor: null, sort: [] } });
  });

  return state;
}

const noHorizontalScroll = (page: Page) =>
  page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth);

async function pick(page: Page, name: string) {
  await page.getByRole('checkbox', { name: `Selectează ${name}`, exact: true }).check();
}

async function shot(page: Page, testInfo: { project: { name: string } }, name: string) {
  // Let the checkbox and modal open/step animations finish so the screenshot shows the settled state.
  await page.waitForTimeout(600);
  await page.screenshot({ path: `${shots}/${testInfo.project.name}-${name}.png` });
}

test('merge button appears from two selected items and the wizard merges them', async ({ page }, testInfo) => {
  const state = await mockApi(page, 204);
  await page.goto('/items');
  await expect(page.getByText('Creion HB', { exact: true })).toBeVisible();
  const mergeButton = page.getByRole('button', { name: /Unește/ });

  await pick(page, 'Creion HB');
  await expect(mergeButton).toHaveCount(0);

  await pick(page, 'Creion HB 12 buc');
  await expect(mergeButton).toHaveText('Unește (2)');
  await shot(page, testInfo, '1-selection');

  await mergeButton.click();
  const modal = page.getByRole('dialog');
  await expect(modal.locator('.ant-steps-item')).toHaveCount(3);
  await expect(modal.getByRole('button', { name: 'Înainte' })).toBeDisabled();
  await shot(page, testInfo, '2-choose-primary');

  await modal.getByText('Creion HB 12 buc').click();
  await shot(page, testInfo, '2b-primary-selected');
  await modal.getByRole('button', { name: 'Înainte' }).click();
  await expect(modal.getByText('Linii din liste de aprovizionare')).toBeVisible();
  await expect(modal.getByText(/2 se vor combina/)).toBeVisible();
  await shot(page, testInfo, '3-affected-data');

  await modal.getByRole('button', { name: 'Înainte' }).click();
  await expect(modal.getByText('Acțiune ireversibilă')).toBeVisible();
  await shot(page, testInfo, '4-confirm');

  const loadsBefore = state.listLoads;
  await modal.getByRole('button', { name: 'Unește articolele' }).click();

  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByText('Itemele au fost unite.')).toBeVisible();
  expect(state.mergeBody).toEqual({ primaryItemId: 'item-2', itemIds: ['item-1', 'item-2'] });
  expect(state.listLoads).toBeGreaterThan(loadsBefore);
  await expect(page.getByRole('button', { name: /Unește/ })).toHaveCount(0);
  expect(await noHorizontalScroll(page)).toBe(true);
});

test('a forbidden merge shows an error and keeps the wizard open', async ({ page }, testInfo) => {
  await mockApi(page, 403);
  await page.goto('/items');
  await expect(page.getByText('Marker negru', { exact: true })).toBeVisible();
  await pick(page, 'Creion HB');
  await pick(page, 'Marker negru');
  await page.getByRole('button', { name: /Unește/ }).click();
  const modal = page.getByRole('dialog');

  await modal.getByText('Marker negru').click();
  await modal.getByRole('button', { name: 'Înainte' }).click();
  await modal.getByRole('button', { name: 'Înainte' }).click();
  await modal.getByRole('button', { name: 'Unește articolele' }).click();

  await expect(page.getByText('Itemele nu au putut fi unite.')).toBeVisible();
  await expect(modal).toBeVisible();
  await expect(modal.getByText('Nu aveți permisiunea de a efectua această acțiune.')).toBeVisible();
  await shot(page, testInfo, '5-forbidden');
});
