import { expect, Page } from '@playwright/test';

export const DEMO_USER = { email: 'demo@docflowhub.com', password: 'Demo123!' };
export const DEMO_ADMIN = { email: 'demo-admin@docflowhub.com', password: 'Demo123!' };

/** Signs in through the real login form (works on the demo, where every other POST is blocked). */
export async function signIn(page: Page, account: { email: string; password: string }) {
  await page.goto('/Account/Login');
  await page.locator('input[name="Input.Email"]').fill(account.email);
  await page.locator('input[name="Input.Password"]').fill(account.password);
  await Promise.all([
    page.waitForURL((url) => !url.pathname.startsWith('/Account/Login')),
    page.locator('form button[type="submit"]').first().click(),
  ]);
}

/** Opens a page and checks it rendered inside the app shell without an error page. */
export async function expectShellPage(page: Page, path: string) {
  const response = await page.goto(path);
  expect(response?.status(), `${path} status`).toBeLessThan(400);
  await expect(page.locator('.dfh-titlebar'), `${path} title bar`).toBeVisible();
  await expect(page.locator('.dfh-rail'), `${path} rail`).toBeVisible();
  await expect(page.locator('.dfh-statusbar'), `${path} status bar`).toBeVisible();
  await expect(page.getByText('Something went wrong'), `${path} error page`).toHaveCount(0);
}
