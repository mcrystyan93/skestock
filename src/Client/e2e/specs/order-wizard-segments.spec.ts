import { expect, test, type Page } from '@playwright/test';

const classId = 'class-1';
const shots = '/tmp/claude-1000/-home-militarucristian-RiderProjects-skestock/a43769f3-b6cd-440c-91b2-1968b05b8c00/scratchpad';
// 1x1 PNG, enough for a thumbnail <img> to load.
const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==', 'base64');
const longName = 'Articol cu un nume extrem de lung fără spații SuperCalifragilisticExpialidociousAbcdefghijklmnopqrstuvwxyz';

const lowStock = [
  { itemId: 'low-1', itemName: 'Creioane', sku: 'CR-1', imageFileId: 'f1', unit: 'buc', locationName: 'Depozit A' },
  { itemId: 'low-2', itemName: 'Hârtie A4', sku: null, imageFileId: 'f2', unit: 'top', locationName: 'Depozit B' },
];

const segmentItem = (itemId: string, name: string, averageQuantity: number) => ({
  itemId,
  name,
  sku: null,
  imageFileId: `img-${itemId}`,
  unit: 'cutie',
  averageQuantity,
  purchaseCount: 5,
  averageIntervalDays: 30,
});

const segments = {
  weekly: [segmentItem('w-1', 'Detergent', 2)],
  everyTwoWeeks: [segmentItem('b-1', 'Săpun lichid', 4)],
  monthly: [segmentItem('m-1', 'Marker', 3), segmentItem('m-2', longName, 7)],
  oncePerClass: [segmentItem('o-1', 'Tablă magnetică', 1)],
};

async function mockApi(page: Page) {
  await page.route('**/api/**', async (route) => {
    const { pathname } = new URL(route.request().url());

    if (pathname === '/api/Users/manage/info') return route.fulfill({ json: { email: 'test@example.com', isEmailConfirmed: true } });
    if (pathname === `/api/SchoolClasses/${classId}`) {
      return route.fulfill({
        json: { id: classId, name: 'Clasa E2E', startDate: '2026-09-01T00:00:00Z', endDate: '2027-06-15T00:00:00Z', status: 'active' },
      });
    }
    if (pathname === `/api/SchoolClasses/${classId}/summary`) return route.fulfill({ json: { id: classId } });
    if (pathname === `/api/Stock/class/${classId}/low-stock`) return route.fulfill({ json: lowStock });
    if (pathname === '/api/statistics/purchases/segments') return route.fulfill({ json: segments });
    if (pathname === '/api/statistics/purchases/items-history') return route.fulfill({ json: [] });
    if (/^\/api\/Items\/[^/]+\/image$/.test(pathname)) return route.fulfill({ body: png, contentType: 'image/png' });
    return route.fulfill({ json: { data: [], hasNextPage: false, sort: [], id: classId, name: 'Clasa E2E' } });
  });
}

const viewports = [
  { name: 'phone', width: 360, height: 740 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'desktop', width: 1280, height: 800 },
];

const noHorizontalScroll = (page: Page) =>
  page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth);

for (const viewport of viewports) {
  test(`order wizard segments at ${viewport.width}x${viewport.height}`, async ({ page }) => {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    await mockApi(page);
    await page.goto(`/school-classes/${classId}?tab=1`);

    await page.getByRole('button', { name: 'Creează comandă' }).first().click();
    const modal = page.getByRole('dialog').first();
    await modal.locator('fieldset').getByText('Comandă nouă').click();
    await modal.getByRole('button', { name: 'Continuă' }).click();
    await expect(modal.getByText('Alege produsele pentru comandă')).toBeVisible();

    const tabs = modal.getByRole('tab');
    await expect(tabs).toHaveCount(5);
    const tab = (title: string) => tabs.filter({ hasText: title });
    // A tab clipped by the narrow tab bar is clicked where it peeks out, or reached through the "..." menu when fully hidden.
    const selectTab = async (title: string) => {
      const [bar, box] = [await modal.locator('.ant-tabs-nav-wrap').boundingBox(), await tab(title).boundingBox()];
      const fullyHidden = !bar || !box || box.x >= bar.x + bar.width || box.x + box.width <= bar.x;
      if (!fullyHidden) return tab(title).dispatchEvent('click');
      await modal.locator('.ant-tabs-nav-more').click();
      await page.locator('.ant-tabs-dropdown-menu-item').filter({ hasText: title }).dispatchEvent('click');
    };

    // Every tab is reachable and shows its thumbnails.
    for (const title of ['Stoc redus', 'Comandate săptămânal', 'La 2 săptămâni', 'Lunar', 'O dată pe clasă']) {
      await selectTab(title);
      await expect(tab(title)).toHaveAttribute('aria-selected', 'true');
      const thumbnails = modal.locator('[role="tabpanel"]:visible ske-item-thumbnail img');
      await expect(thumbnails.first()).toBeVisible();
      await expect
        .poll(() => thumbnails.evaluateAll((images) => images.every((img) => (img as HTMLImageElement).naturalWidth > 0)))
        .toBe(true);
      expect(await noHorizontalScroll(page), `horizontal scroll on ${title}`).toBe(true);
    }

    // Stoc redus: both start ticked; drop Hârtie A4, so only Creioane stays.
    await selectTab('Stoc redus');
    await modal.getByRole('checkbox', { name: /Hârtie A4/ }).uncheck();
    await expect(modal.getByText('1 din 2 selectate')).toBeVisible();

    // Lunar: tick Marker only.
    await selectTab('Lunar');
    await modal.getByRole('checkbox', { name: /Marker/ }).check();
    await expect(modal.getByText('1 din 2 selectate')).toBeVisible();
    await page.waitForTimeout(600); // let the tab slide animation settle for the screenshot
    await page.screenshot({ path: `${shots}/order-wizard-segments-${viewport.width}.png` });
    expect(await noHorizontalScroll(page)).toBe(true);

    // Details step, then the editor.
    await modal.getByRole('button', { name: 'Continuă' }).click();
    await modal.getByPlaceholder('Numele comenzii').fill('Comandă segmente');
    await modal.getByRole('button', { name: 'Continuă' }).click();

    const quantities = modal.locator('nz-input-number input');
    await expect(quantities).toHaveCount(2);
    await expect(modal.locator('nz-input-number[aria-label*="Creioane"] input')).toHaveValue('1');
    await expect(modal.locator('nz-input-number[aria-label*="Marker"] input')).toHaveValue('3');
    await expect(modal.getByText('Hârtie A4')).toHaveCount(0);
    await expect(modal.getByText('Detergent')).toHaveCount(0);
    expect(await noHorizontalScroll(page)).toBe(true);
  });
}
