import { expect, test } from '@playwright/test';
import { DEMO_ADMIN, DEMO_USER, expectShellPage, signIn } from './helpers';

// Smoke: both demo accounts can sign in and every main destination renders in the shell.

const USER_PAGES = ['/', '/Documents', '/Projects', '/Teams', '/Settings/AI', '/Account/Manage'];
const ADMIN_PAGES = ['/Admin', '/Admin/Users', '/Admin/Roles', '/Admin/UserLimits', '/Admin/Analytics', '/Admin/Settings'];

test('demo user: main pages render in the shell', async ({ page }) => {
  await signIn(page, DEMO_USER);
  for (const path of USER_PAGES) {
    await expectShellPage(page, path);
  }
  // No admin rail item for a regular user.
  await expect(page.locator('.dfh-rail [aria-label="Admin"]')).toHaveCount(0);
});

test('demo user: admin area shows the 403 page', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Admin');
  await expect(page.locator('.dfh-error-code')).toHaveText(/403/);
});

test('demo admin: admin pages render with the admin nav', async ({ page }) => {
  await signIn(page, DEMO_ADMIN);
  for (const path of ADMIN_PAGES) {
    await expectShellPage(page, path);
    await expect(page.locator('.dfh-toolwindow--admin'), `${path} admin nav`).toBeVisible();
  }
  await expect(page.locator('.dfh-rail [aria-label="Admin"]')).toHaveCount(1);
});

test('unknown address shows the 404 page', async ({ page }) => {
  const response = await page.goto('/this-page-does-not-exist');
  expect(response?.status()).toBe(404);
  await expect(page.locator('.dfh-error-code')).toHaveText(/404/);
});

// Demo-only checks: the layout's antiforgery token is accepted, and blocked writes explain themselves.
test('demo: AJAX write with the layout token is blocked as read-only (403), not rejected (400)', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  const status = await page.evaluate(async () => {
    const token = (document.querySelector('meta[name="csrf-token"]') as HTMLMetaElement).content;
    const response = await fetch('/Documents/Index?handler=BulkDelete', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', RequestVerificationToken: token },
      body: JSON.stringify({ documentIds: [] }),
    });
    return response.status;
  });
  expect(status).toBe(403);
});

test('demo: a blocked form post shows the read-only toast', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  await page.evaluate(() => {
    const token = (document.querySelector('meta[name="csrf-token"]') as HTMLMetaElement).content;
    const form = document.createElement('form');
    form.method = 'post';
    form.action = '/Documents?handler=Delete';
    form.innerHTML = `<input name="__RequestVerificationToken" value="${token}"><input name="documentId" value="1">`;
    document.body.appendChild(form);
    form.submit();
  });
  await expect(page.locator('.dfh-toast')).toContainText('read-only');
});

test('Ctrl K focuses the global search', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  await page.keyboard.press('Control+k');
  await expect(page.locator('#dfh-global-search')).toBeFocused();
});
