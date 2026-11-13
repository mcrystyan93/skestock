import {expect, test, type Page} from '@playwright/test';
import {saveScreenshotsAfterEach} from '../screenshots';

saveScreenshotsAfterEach();

const classId = 'class-1';

const stat = (itemId: string, itemName: string, totalQuantity: number, purchaseCount: number) => ({
  itemId,
  itemName,
  sku: null,
  unit: 'kg',
  categoryName: 'Alimente',
  totalQuantity,
  totalValue: totalQuantity * 2,
  purchaseCount,
  averageQuantity: totalQuantity / purchaseCount,
  averageUnitPrice: 2,
  lastPurchasedAt: '2026-09-01T00:00:00Z',
});

const donations = {
  computedAt: '2026-09-26T12:00:00Z',
  byQuantity: [stat('a', 'Mere donate', 12, 2), stat('b', 'Pere donate', 5, 1)],
  byValue: [stat('a', 'Mere donate', 12, 2)],
  byFrequency: [stat('a', 'Mere donate', 12, 2)],
};

const emptyPurchases = {computedAt: null, byQuantity: [], byValue: [], byFrequency: []};

async function mockApi(page: Page, requests: string[], payload: unknown = donations) {
  await page.route('**/api/**', async (route) => {
    const url = new URL(route.request().url());
    const {pathname} = url;

    if (pathname === '/api/Users/manage/info') return route.fulfill({
      json: {
        email: 'test@example.com',
        isEmailConfirmed: true
      }
    });
    if (pathname === `/api/SchoolClasses/${classId}`) {
      return route.fulfill({
        json: {
          id: classId,
          name: 'Clasa E2E',
          startDate: '2026-09-01T00:00:00Z',
          endDate: '2027-06-15T00:00:00Z',
          status: 'active'
        },
      });
    }
    if (pathname === `/api/SchoolClasses/${classId}/summary`) return route.fulfill({json: {id: classId}});
    if (pathname === '/api/statistics/donations/top') {
      requests.push(url.search);
      return route.fulfill({json: payload});
    }
    if (pathname === '/api/statistics/purchases/top') return route.fulfill({json: emptyPurchases});
    return route.fulfill({
      json: {
        data: [],
        hasNextPage: false,
        sort: [],
        labels: [],
        series: [],
        points: [],
        id: classId,
        name: 'Clasa E2E'
      }
    });
  });
}

test('top donations card loads for the current class and switches scope', async ({page}) => {
  const requests: string[] = [];
  await mockApi(page, requests);
  await page.setViewportSize({width: 1280, height: 900});
  await page.goto(`/school-classes/${classId}?tab=4`);

  const card = page.locator('ske-top-donations-card');
  await expect(card.getByText('Top donații')).toBeVisible();
  await expect(card.locator('apx-chart, .apexcharts-canvas').first()).toBeVisible();
  expect(requests[0]).toContain('scope=Class');
  expect(requests[0]).toContain(`classId=${classId}`);

  await card.getByText('90 de zile').click();
  await expect.poll(() => requests.some((r) => r.includes('scope=Last90Days'))).toBe(true);

});

test('top donations card names donations in its empty state', async ({page}) => {
  await mockApi(page, [], emptyPurchases);
  await page.goto(`/school-classes/${classId}?tab=4`);

  await expect(page.locator('ske-top-donations-card').getByText('Nu există donații pentru filtrele selectate.')).toBeVisible();
});
