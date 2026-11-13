import { test } from '@playwright/test';
import { DEMO_ADMIN, DEMO_USER, signIn } from './helpers';

// Full-page screenshots at 1280×800 for the sprint log (compare with the design mocks).
//   SHOTS_DIR=<folder> BASE_URL=<url> npx playwright test screenshots.spec.ts
const dir = process.env.SHOTS_DIR ?? './screenshots';

const USER_PAGES: Array<[string, string]> = [
  ['dashboard', '/'],
  ['documents', '/Documents'],
  ['upload', '/Documents/Upload'],
  ['projects', '/Projects'],
  ['teams', '/Teams'],
  ['settings-ai', '/Settings/AI'],
  ['profile', '/Account/Manage'],
  ['not-found', '/this-page-does-not-exist'],
];
const ADMIN_PAGES: Array<[string, string]> = [
  ['admin-overview', '/Admin'],
  ['admin-users', '/Admin/Users'],
  ['admin-analytics', '/Admin/Analytics'],
];

test('user screens', async ({ page }) => {
  await signIn(page, DEMO_USER);
  for (const [name, path] of USER_PAGES) {
    await page.goto(path);
    await page.screenshot({ path: `${dir}/${name}.png` });
  }
  // A document and a team, picked from their lists.
  for (const [name, list, pattern] of [
    ['document-details', '/Documents', '/Documents/Details/'],
    ['team', '/Teams', '/Teams/Details/'],
  ] as const) {
    await page.goto(list);
    const href = await page.locator(`a[href*="${pattern}"]`).first().getAttribute('href');
    if (href) {
      await page.goto(href);
      await page.screenshot({ path: `${dir}/${name}.png` });
    }
  }
});

test('admin screens', async ({ page }) => {
  await signIn(page, DEMO_ADMIN);
  for (const [name, path] of ADMIN_PAGES) {
    await page.goto(path);
    await page.screenshot({ path: `${dir}/${name}.png` });
  }
});

test('signed-out screens', async ({ page }) => {
  for (const [name, path] of [['sign-in', '/Account/Login'], ['error-404-anonymous', '/nope']] as const) {
    await page.goto(path);
    await page.screenshot({ path: `${dir}/${name}.png` });
  }
});
