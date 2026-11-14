import { expect, test } from '@playwright/test';
import { DEMO_USER, signIn } from './helpers';

// Documents list writes (delete, bulk delete, move). They change data, so they run only on a
// non-demo instance seeded with the demo data (start once with DemoMode on, then restart with it off).

test.beforeEach(async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  test.skip((await page.locator('[data-dfh-bulk="move"]').count()) === 0, 'writes are disabled in the demo');
});

test('bulk delete reports each document and skips the ones the user does not own', async ({ page }) => {
  const owned = page.locator('tbody tr[data-owner="1"]').first();
  const foreign = page.locator('tbody tr[data-owner="0"]').first();
  const ownedId = (await owned.getAttribute('data-id'))!;
  await owned.locator('.dfh-row-check').check();
  await foreign.locator('.dfh-row-check').check();
  await page.locator('[data-dfh-bulk="delete"]').click();

  const modal = page.locator('#dfh-bulk-delete-modal');
  await expect(modal.locator('.modal-title')).toHaveText('Delete 2 documents?');
  await modal.locator('[data-dfh-bulk-confirm]').click();
  await expect(modal.locator('[data-dfh-bulk-caption]')).toHaveText('1 deleted · 1 skipped · done');
  await expect(modal.locator('.dfh-result.is-danger')).toHaveText('not owner · skipped');
  await modal.locator('[data-dfh-bulk-close]').click();

  await expect(page.locator('.dfh-toast')).toContainText('Deleted 1 document');
  await expect(page.locator(`tbody tr[data-id="${ownedId}"]`)).toHaveCount(0);
});

test('move puts a document into the chosen folder', async ({ page }) => {
  const row = page.locator('tbody tr[data-owner="1"]').first();
  const id = (await row.getAttribute('data-id'))!;
  await row.click();
  await page.locator('#dfh-inspector-body [data-bs-toggle="dropdown"]').click();
  await page.locator('#dfh-inspector-body [data-dfh-action="move"]').click();

  const modal = page.locator('#dfh-move-modal');
  await modal.locator('[data-dfh-move-filter]').fill('draft');
  const target = modal.locator('.dfh-picker-row.is-selected');
  await expect(target).toHaveCount(1);
  const path = (await target.getAttribute('data-path'))!;
  await modal.locator('[data-dfh-move-confirm]').click();

  await expect(page.locator('.dfh-toast')).toContainText(`Moved 1 document to ${path}`);
  await page.locator(`tbody tr[data-id="${id}"]`).click();
  await expect(page.locator('#dfh-inspector-body .dfh-insp-path')).toHaveText(path);
});

test('delete removes one document after confirmation', async ({ page }) => {
  const row = page.locator('tbody tr[data-owner="1"]').first();
  const id = (await row.getAttribute('data-id'))!;
  const title = (await row.getAttribute('data-title'))!;
  await row.click();
  await page.locator('#dfh-inspector-body [data-bs-toggle="dropdown"]').click();
  await page.locator('#dfh-inspector-body [data-dfh-action="delete"]').click();

  const modal = page.locator('#dfh-delete-modal');
  await expect(modal.locator('.modal-title')).toHaveText(`Delete “${title}”?`);
  await modal.locator('button[type="submit"]').click();

  await expect(page.locator('.dfh-toast')).toContainText(`Deleted "${title}"`);
  await expect(page.locator(`tbody tr[data-id="${id}"]`)).toHaveCount(0);
});
