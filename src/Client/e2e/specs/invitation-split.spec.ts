import path from 'node:path';
import {expect, Page, test} from '@playwright/test';
import {skipUnlessLoggedIn} from '../auth';
import {saveScreenshotsAfterEach, screenshotRoot} from '../screenshots';

saveScreenshotsAfterEach();

test.use({ignoreHTTPSErrors: true});

type Split = {
  studentInvitationCount: number;
  instructorInvitationCount: number;
  openingPrayerInvitationCount: number;
  experienceInvitationCount: number;
  changeInvitationCount: number;
};

const LABELS = ['Cursanți', 'Instructori', 'Rugăciune deschidere', 'Experiențe', 'Schimbări'];
const SECTION = 'Împărțirea invitațiilor';

async function openConfiguration(page: Page): Promise<void> {
  const {email, password} = skipUnlessLoggedIn(test);
  await page.route('**/api/ClassConfiguration/invitations', (route) =>
    route.request().method() === 'GET'
      ? route.fulfill({status: 200, json: {invitationCount: 30}})
      : route.fallback(),
  );

  await page.goto('/login');
  await page.getByPlaceholder('Email').fill(email);
  await page.getByPlaceholder('Parola').fill(password);
  await page.getByRole('button', {name: 'Login'}).click();
  await expect(page).toHaveURL(/school-classes/);

  const menuToggle = page.getByRole('button', {name: 'Deschide meniul'});
  if (await menuToggle.isVisible()) await menuToggle.click();
  await page.getByText('Configurare', {exact: true}).click();
  await expect(page).toHaveURL(/configuration/);
}

const section = (page: Page) => page.getByRole('region', {name: SECTION});
const saveButton = (page: Page) => section(page).getByRole('button', {name: 'Salvează'});
const total = (page: Page) => section(page).getByTestId('invitation-split-total');

test('administrator edits, validates and saves the invitation split', async ({page}) => {
  let saved: Split = {
    studentInvitationCount: 10,
    instructorInvitationCount: 5,
    openingPrayerInvitationCount: 2,
    experienceInvitationCount: 3,
    changeInvitationCount: 4,
  };
  const putRequests: Split[] = [];
  let getRequests = 0;
  await page.route('**/api/ClassConfiguration/invitation-split', async (route) => {
    if (route.request().method() === 'GET') {
      getRequests++;
      await route.fulfill({status: 200, json: saved});
      return;
    }
    saved = route.request().postDataJSON() as Split;
    putRequests.push(saved);
    await route.fulfill({status: 200, json: saved});
  });

  await openConfiguration(page);
  const inputs = LABELS.map((label) => section(page).getByLabel(label, {exact: true}));
  await expect(inputs[0]).toHaveValue('10');
  await expect(total(page)).toHaveText(/Total alocat:\s*24\s*din\s*30/);

  await inputs[0].focus();
  for (const input of inputs.slice(1)) {
    await page.keyboard.press('Tab');
    await expect(input).toBeFocused();
  }

  // The number input clamps to its maximum on blur, so 201 never reaches the form validator.
  await inputs[0].fill('201');
  await inputs[0].blur();
  await expect(inputs[0]).toHaveValue('200');
  await expect(total(page)).toHaveText(/Total alocat:\s*214\s*din\s*30/);

  await inputs[0].fill('');
  await saveButton(page).click();
  await expect(section(page).getByText('Numărul de invitații este obligatoriu.')).toBeVisible();
  // Like the rooms card, the aria attributes sit on the nz-input-number host.
  await expect(section(page).locator('nz-input-number').first()).toHaveAttribute('aria-invalid', 'true');
  await expect(section(page).getByText('Introduceți un număr întreg.')).toHaveCount(0);
  await page.screenshot({
    path: path.join(screenshotRoot, 'invitation-split', `${test.info().project.name}-validation-error.png`),
    fullPage: true,
  });
  expect(putRequests).toHaveLength(0);

  const values = ['12', '6', '1', '0', '11'];
  for (const [index, value] of values.entries()) await inputs[index].fill(value);
  await saveButton(page).click();
  await expect.poll(() => putRequests.length).toBe(1);
  expect(putRequests[0]).toEqual({
    studentInvitationCount: 12,
    instructorInvitationCount: 6,
    openingPrayerInvitationCount: 1,
    experienceInvitationCount: 0,
    changeInvitationCount: 11,
  });
  await expect.poll(() => getRequests).toBe(2);
  await expect(total(page)).toHaveText(/Total alocat:\s*30\s*din\s*30/);

  const viewportWidth = await page.evaluate(() => window.innerWidth);
  const boxes = await section(page).getByRole('spinbutton').evaluateAll((elements) =>
    elements.map((element) => element.getBoundingClientRect().right),
  );
  expect(boxes).toHaveLength(5);
  for (const right of boxes) expect(right).toBeLessThanOrEqual(viewportWidth);
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(viewportWidth);

  await page.reload();
  await expect.poll(() => getRequests).toBe(3);
  for (const [index, value] of values.entries()) {
    await expect(section(page).getByLabel(LABELS[index], {exact: true})).toHaveValue(value);
  }
});

test('shows the server error and keeps the edit when saving fails', async ({page}) => {
  const saved: Split = {
    studentInvitationCount: 10,
    instructorInvitationCount: 5,
    openingPrayerInvitationCount: 2,
    experienceInvitationCount: 3,
    changeInvitationCount: 4,
  };
  await page.route('**/api/ClassConfiguration/invitation-split', (route) =>
    route.request().method() === 'GET'
      ? route.fulfill({status: 200, json: saved})
      : route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        json: {status: 500, title: 'A apărut o eroare la salvare.'},
      }),
  );

  await openConfiguration(page);
  await section(page).getByLabel('Cursanți', {exact: true}).fill('50');
  await saveButton(page).click();

  await expect(section(page).getByText('A apărut o eroare la salvare.').first()).toBeVisible();
  // The store keeps the confirmed values; the form keeps the user's edit so it can be retried.
  await expect(section(page).getByLabel('Cursanți', {exact: true})).toHaveValue('50');
  await expect(saveButton(page)).toBeEnabled();
});

test('shows the loader while the split is loading', async ({page}) => {
  let release: () => void = () => undefined;
  const pending = new Promise<void>((resolve) => (release = resolve));
  await page.route('**/api/ClassConfiguration/invitation-split', async (route) => {
    await pending;
    await route.fulfill({
      status: 200,
      json: {
        studentInvitationCount: 1,
        instructorInvitationCount: 1,
        openingPrayerInvitationCount: 1,
        experienceInvitationCount: 1,
        changeInvitationCount: 1,
      },
    });
  });

  await openConfiguration(page);
  const card = section(page).locator('nz-card');
  await expect(card).toHaveAttribute('aria-busy', 'true');
  await expect(section(page).getByRole('spinbutton')).toHaveCount(0);
  await section(page).scrollIntoViewIfNeeded();
  await page.screenshot({
    path: path.join(screenshotRoot, 'invitation-split', `${test.info().project.name}-loading.png`),
    fullPage: true,
  });

  release();
  await expect(card).toHaveAttribute('aria-busy', 'false');
  await expect(section(page).getByLabel('Cursanți', {exact: true})).toHaveValue('1');
});
