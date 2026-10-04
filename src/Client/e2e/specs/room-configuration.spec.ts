import { expect, test } from '@playwright/test';

test.use({ ignoreHTTPSErrors: true });

test('administrator validates and saves seat counts for all rooms', async ({ page }) => {
  let savedConfiguration = { room4SeatCount: 12, room2SeatCount: 3, room6SeatCount: 6 };
  const putRequests: Array<typeof savedConfiguration> = [];
  let getRequests = 0;
  await page.route('**/api/ClassConfiguration/rooms', async (route) => {
    if (route.request().method() === 'GET') {
      getRequests++;
      await route.fulfill({ status: 200, json: savedConfiguration });
      return;
    }

    savedConfiguration = route.request().postDataJSON() as typeof savedConfiguration;
    putRequests.push(savedConfiguration);
    await route.fulfill({ status: 200, json: savedConfiguration });
  });

  await page.goto('/login');
  await page.getByPlaceholder('Email').fill('administrator@localhost');
  await page.getByPlaceholder('Parola').fill('Administrator1!');
  await page.getByRole('button', { name: 'Login' }).click();
  await expect(page).toHaveURL(/school-classes/);
  await page.route('**/api/Users/manage/info', (route) =>
    route.fulfill({
      status: 200,
      json: { email: 'administrator@localhost', isEmailConfirmed: true },
    }),
  );

  const menuToggle = page.getByRole('button', { name: 'Deschide meniul' });
  if (await menuToggle.isVisible()) await menuToggle.click();
  await page.getByText('Configurare', { exact: true }).click();
  await expect(page).toHaveURL(/configuration/);
  await expect(page.getByRole('heading', { name: 'Numărul de locuri' }).last()).toBeVisible();
  const room4 = page.getByLabel('Sala 4 — principală');
  const room2 = page.getByLabel('Sala 2 — secundară');
  const room6 = page.getByLabel('Sala 6 — secundară');
  await expect(room4).toHaveValue('12');

  await room4.focus();
  await page.keyboard.press('Tab');
  await expect(room2).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(room6).toBeFocused();

  await room4.fill('201');
  await page.getByRole('button', { name: 'Salvează' }).last().click();
  await expect(page.getByText('Valoarea maximă este 200.')).toBeVisible();
  expect(putRequests).toHaveLength(0);

  await room4.fill('0');
  await room2.fill('200');
  await room6.fill('42');
  await page.getByRole('button', { name: 'Salvează' }).last().click();
  await expect.poll(() => putRequests.length).toBe(1);
  await expect.poll(() => getRequests).toBe(2);
  await expect(page.getByRole('button', { name: 'Salvează' }).last()).toBeVisible();
  await expect(room4).toHaveValue('0');
  await expect(room2).toHaveValue('200');
  await expect(room6).toHaveValue('42');

  const inputBoxes = await page.locator('input[type="number"]').evaluateAll((inputs) =>
    inputs.map((input) => {
      const { x, y, width, height } = input.getBoundingClientRect();
      return { x, y, width, height };
    }),
  );
  expect(inputBoxes).toHaveLength(3);
  for (const box of inputBoxes) {
    expect(box.width).toBeGreaterThan(0);
    expect(box.height).toBeGreaterThan(0);
    expect(box.x + box.width).toBeLessThanOrEqual(await page.evaluate(() => window.innerWidth));
  }

  await page.reload();
  await expect(page.getByRole('heading', { name: 'Numărul de locuri' }).last()).toBeVisible();
  await expect.poll(() => getRequests).toBe(3);
  await expect(page.getByLabel('Sala 4 — principală')).toHaveValue('0');
  await expect(page.getByLabel('Sala 2 — secundară')).toHaveValue('200');
  await expect(page.getByLabel('Sala 6 — secundară')).toHaveValue('42');
});
