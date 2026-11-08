import { expect, test, type Page } from '@playwright/test';
import { saveScreenshotsAfterEach, screenshotRoot } from '../screenshots';

saveScreenshotsAfterEach();

const classId = 'class-1';
const shots = `${screenshotRoot}/order-wizard-segments`;
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
      // Scoped through the tab itself: the modal also holds another tab bar at desktop width.
      const nav = tab(title).locator('xpath=ancestor::nz-tabs-nav');
      // Both rects are read in one evaluate so the modal's open animation cannot skew one against the other.
      const fullyHidden = await tab(title).evaluate((element) => {
        const bar = element.closest('.ant-tabs-nav-wrap')?.getBoundingClientRect();
        const box = element.getBoundingClientRect();
        return !bar || box.left >= bar.right || box.right <= bar.left;
      });
      if (!fullyHidden) return tab(title).dispatchEvent('click');
      const item = page.locator('.ant-tabs-dropdown-menu-item').filter({ hasText: title });
      // The "..." menu opens on mouseenter (a click only toggles it, racing that), so re-enter the button until the item shows.
      await expect(async () => {
        await page.mouse.move(0, 0);
        await nav.locator('.ant-tabs-nav-more').hover();
        await expect(item).toBeVisible({ timeout: 1_000 });
        // Inside the retry: the menu closes on mouseleave, so the item can vanish between check and click.
        await item.dispatchEvent('click', undefined, { timeout: 1_000 });
      }).toPass();
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

    // Inactive tab panels stay in the DOM (and two overlap mid-slide), so counts and checkboxes are read from the named panel.
    const panel = (title: string) => modal.getByRole('tabpanel', { name: title });

    // Stoc redus: nothing starts ticked; tick Creioane only.
    await selectTab('Stoc redus');
    await expect(panel('Stoc redus').getByText('0 din 2 selectate')).toBeVisible();
    await panel('Stoc redus').getByRole('checkbox', { name: /Creioane/ }).check();
    await expect(panel('Stoc redus').getByText('1 din 2 selectate')).toBeVisible();

    // Lunar: tick Marker only.
    await selectTab('Lunar');
    await panel('Lunar').getByRole('checkbox', { name: /Marker/ }).check();
    await expect(panel('Lunar').getByText('1 din 2 selectate')).toBeVisible();
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
    // At desktop width the editor also lists suggestions, so only the order lines are checked.
    const orderLines = modal.locator('ske-order-list-line');
    await expect(orderLines).toHaveCount(2);
    await expect(orderLines.getByText('Hârtie A4')).toHaveCount(0);
    await expect(orderLines.getByText('Detergent')).toHaveCount(0);
    expect(await noHorizontalScroll(page)).toBe(true);
  });
}
