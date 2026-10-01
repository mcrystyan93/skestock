import { expect, test, type Request } from '@playwright/test';

test.use({ trace: 'off' });

test.describe('login form validation', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/Users/manage/info', (route) => route.fulfill({ status: 401 }));
  });

  test('requires email and password without sending a login request', async ({ page }) => {
    const loginRequests: Request[] = [];
    page.on('request', (request) => {
      if (new URL(request.url()).pathname === '/api/Users/login') {
        loginRequests.push(request);
      }
    });

    await page.goto('/login');
    await page.getByRole('button', { name: 'Login' }).click();

    await expect(page.getByText('Email este obligatoriu')).toBeVisible();
    await expect(page.getByText('Parola este obligatorie')).toBeVisible();
    expect(loginRequests).toHaveLength(0);
  });

  test('rejects an invalid email without sending a login request', async ({ page }) => {
    const loginRequests: Request[] = [];
    page.on('request', (request) => {
      if (new URL(request.url()).pathname === '/api/Users/login') {
        loginRequests.push(request);
      }
    });

    await page.goto('/login');
    await page.getByPlaceholder('Email').fill('invalid-email');
    await page.getByPlaceholder('Parola').fill('not-a-real-password');
    await page.getByRole('button', { name: 'Login' }).click();

    await expect(page.getByText('Email invalid')).toBeVisible();
    expect(loginRequests).toHaveLength(0);
  });

  test('toggles password visibility without clearing the password', async ({ page }) => {
    await page.goto('/login');
    const passwordInput = page.getByPlaceholder('Parola');
    await passwordInput.fill('example-password');

    await page.getByRole('button', { name: 'eye-invisible' }).click();

    await expect(passwordInput).toHaveAttribute('type', 'text');
    await expect(passwordInput).toHaveValue('example-password');

    await page.getByRole('button', { name: 'eye' }).click();
    await expect(passwordInput).toHaveAttribute('type', 'password');
    await expect(passwordInput).toHaveValue('example-password');
  });
});

test.describe('login backend errors', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/Users/manage/info', (route) => route.fulfill({ status: 401 }));
    await page.goto('/login');
    await page.getByPlaceholder('Email').fill('person@example.com');
    await page.getByPlaceholder('Parola').fill('example-password');
  });

  test('shows backend validation errors and correlation ID', async ({ page }) => {
    await page.route('**/api/Users/login?useCookies=true', (route) =>
      route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          status: 400,
          title: 'Datele trimise nu sunt valide.',
          error: {
            code: 'validation.failed',
            errors: [{ field: 'Email', code: 'validation.required' }],
            diagnostics: { correlationId: 'login-validation-e2e' },
          },
        }),
      }),
    );

    await page.getByRole('button', { name: 'Login' }).click();

    await expect(page.getByText('Datele trimise nu sunt valide.')).toBeVisible();
    await expect(page.getByText('Câmpul este obligatoriu.')).toBeVisible();
    await expect(page.getByText('Correlation ID: login-validation-e2e')).toBeVisible();
  });

  test('localizes the standard ASP.NET Identity invalid-login response', async ({ page }) => {
    await page.route('**/api/Users/login?useCookies=true', (route) =>
      route.fulfill({
        status: 401,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          status: 401,
          title: 'Invalid login attempt.',
        }),
      }),
    );

    await page.getByRole('button', { name: 'Login' }).click();

    await expect(page.getByText('Adresa de e-mail sau parola sunt incorecte.')).toBeVisible();
    await expect(page.getByText('Invalid login attempt.')).toHaveCount(0);
  });

  for (const status of [500, 503]) {
    test(`shows a localized message for backend status ${status}`, async ({ page }) => {
      const correlationId = `login-server-${status}-e2e`;
      await page.route('**/api/Users/login?useCookies=true', (route) =>
        route.fulfill({
          status,
          contentType: 'application/problem+json',
          body: JSON.stringify({
            status,
            title: 'Internal Server Error',
            error: {
              code: 'common.unexpected',
              diagnostics: { correlationId },
            },
          }),
        }),
      );

      await page.getByRole('button', { name: 'Login' }).click();

      await expect(
        page.getByText('A apărut o eroare neașteptată. Vă rugăm să încercați din nou.'),
      ).toBeVisible();
      await expect(page.getByText(`Correlation ID: ${correlationId}`)).toBeVisible();
      await expect(page.getByText('Internal Server Error')).toHaveCount(0);
    });
  }

  test('shows a generic message when the login request cannot reach the backend', async ({ page }) => {
    await page.route('**/api/Users/login?useCookies=true', (route) => route.abort('failed'));

    await page.getByRole('button', { name: 'Login' }).click();

    await expect(page.getByText('A apărut o eroare neașteptată.')).toBeVisible();
  });

  test('does not allow protected navigation after rejected credentials', async ({ page }) => {
    await page.route('**/api/Users/login?useCookies=true', (route) =>
      route.fulfill({
        status: 401,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          status: 401,
          error: { code: 'auth.invalid_credentials' },
        }),
      }),
    );

    await page.getByRole('button', { name: 'Login' }).click();
    await expect(page.getByText('Adresa de e-mail sau parola sunt incorecte.')).toBeVisible();

    await page.evaluate(() => {
      window.history.pushState(null, '', '/school-classes');
      window.dispatchEvent(new PopStateEvent('popstate'));
    });

    await expect(page).toHaveURL(/\/login$/);
  });

  test('keeps the form disabled and ignores a second submit while the request is pending', async ({ page }) => {
    let requestCount = 0;
    let releaseResponse!: () => void;
    const responseGate = new Promise<void>((resolve) => {
      releaseResponse = resolve;
    });

    await page.route('**/api/Users/login?useCookies=true', async (route) => {
      requestCount++;
      await responseGate;
      await route.fulfill({
        status: 401,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          status: 401,
          error: { code: 'auth.invalid_credentials' },
        }),
      });
    });

    const submitButton = page.getByRole('button', { name: 'Login' });
    await submitButton.click();
    await expect(submitButton).toBeDisabled();
    await page.getByPlaceholder('Email').press('Enter');
    const requestsAfterSecondSubmit = requestCount;
    releaseResponse();

    await expect(page.getByText('Adresa de e-mail sau parola sunt incorecte.')).toBeVisible();
    expect(requestsAfterSecondSubmit).toBe(1);
  });

  test('clears a previous backend error and allows retry', async ({ page }) => {
    let requestCount = 0;
    let releaseRetry!: () => void;
    const retryGate = new Promise<void>((resolve) => {
      releaseRetry = resolve;
    });

    await page.route('**/api/Users/login?useCookies=true', async (route) => {
      requestCount++;
      if (requestCount === 2) await retryGate;
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          status: 500,
          error: { code: 'common.unexpected' },
        }),
      });
    });

    const submitButton = page.getByRole('button', { name: 'Login' });
    await submitButton.click();
    const errorMessage = page.getByText(
      'A apărut o eroare neașteptată. Vă rugăm să încercați din nou.',
    );
    await expect(errorMessage).toBeVisible();

    await submitButton.click();
    await expect(submitButton).toBeDisabled();
    await expect(errorMessage).toHaveCount(0);
    releaseRetry();

    await expect(errorMessage).toBeVisible();
    expect(requestCount).toBe(2);
  });
});

test.describe('live login integration', () => {
  test('rejects a nonexistent identity through the real backend', async ({ page }) => {
    await page.goto('/login');
    await page.getByPlaceholder('Email').fill('nobody.login.e2e@example.invalid');
    await page.getByPlaceholder('Parola').fill('not-a-real-password');

    const loginResponsePromise = page.waitForResponse(
      (response) =>
        new URL(response.url()).pathname === '/api/Users/login' &&
        response.request().method() === 'POST',
    );
    await page.getByRole('button', { name: 'Login' }).click();

    const loginResponse = await loginResponsePromise;
    expect(loginResponse.status()).toBe(401);
    await expect(page.getByText('Adresa de e-mail sau parola sunt incorecte.')).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });

  test('keeps a successful cookie session after reload', async ({ page }) => {
    const email = process.env['PLAYWRIGHT_LOGIN_EMAIL'];
    const password = process.env['PLAYWRIGHT_LOGIN_PASSWORD'];
    test.skip(!email || !password, 'Set dedicated PLAYWRIGHT_LOGIN_EMAIL and PLAYWRIGHT_LOGIN_PASSWORD.');
    if (!email || !password) return;

    await page.goto('/login');
    await page.getByPlaceholder('Email').fill(email);
    await page.getByPlaceholder('Parola').fill(password);

    const loginResponsePromise = page.waitForResponse(
      (response) =>
        new URL(response.url()).pathname === '/api/Users/login' &&
        response.request().method() === 'POST',
    );
    await page.getByRole('button', { name: 'Login' }).click();

    expect((await loginResponsePromise).status()).toBe(200);
    await page.waitForURL(/\/school-classes\/?$/);

    const userInfoResponsePromise = page.waitForResponse(
      (response) => new URL(response.url()).pathname === '/api/Users/manage/info',
    );
    await page.reload();

    expect((await userInfoResponsePromise).status()).toBe(200);
    await expect(page).toHaveURL(/\/school-classes\/?$/);
  });
});
