import { expect, test } from '@playwright/test';
import { DEMO_ADMIN, DEMO_USER, signIn } from './helpers';

// Projects, folders and teams writes. Like documents-actions.spec.ts they run only on a non-demo
// instance seeded with the demo data.

test.beforeEach(async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Projects');
  test.skip((await page.locator('a', { hasText: 'New project' }).count()) === 0, 'writes are disabled in the demo');
});

async function confirm(page: import('@playwright/test').Page) {
  await page.locator('#dfh-confirm-modal [data-dfh-confirm-ok]').click();
}

test('project: create, folders, drag a folder, folder settings, delete, archive, restore, delete', async ({ page }) => {
  const project = `E2E project ${Date.now()}`;
  await page.goto('/Projects/Create');
  await page.locator('input[name="Name"]').fill(project);
  await page.locator('.dfh-color-option').nth(2).click();
  await page.locator('button', { hasText: 'Create project' }).click();
  await expect(page.locator('.dfh-toast')).toContainText(`Created project "${project}"`);
  await expect(page.locator('.dfh-breadcrumb-current')).toHaveText(project);

  for (const name of ['Alpha', 'Beta']) {
    await page.locator('.dfh-tree a.dfh-tree-link', { hasText: project }).click();
    await page.locator('.dfh-toolbar a', { hasText: 'New folder' }).click();
    await page.locator('input[name="Name"]').fill(name);
    await page.locator('button', { hasText: 'Create folder' }).click();
    await expect(page.locator('.dfh-toast')).toContainText(`Created folder "${name}"`);
  }

  // Drag Beta onto Alpha in the project tree.
  const beta = page.locator('.dfh-tree-row[data-dfh-drag-folder]', { hasText: 'Beta' });
  const alpha = page.locator('.dfh-tree-row[data-dfh-drag-folder]', { hasText: 'Alpha' });
  await beta.dragTo(alpha);
  await expect(page.locator('.dfh-toast')).toContainText('into Alpha');
  await page.locator('.dfh-tree a.dfh-tree-link', { hasText: 'Beta' }).click();
  await expect(page.locator('.dfh-breadcrumb')).toContainText('Alpha');

  // Folder settings: rename and archive.
  await page.locator('#Name').fill('Beta 2');
  await page.locator('#IsArchived').check();
  await page.locator('form[action*="handler=Save"] button[type="submit"]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Folder saved and archived');
  await expect(page.locator('.dfh-breadcrumb-current')).toHaveText('Beta 2');

  // Delete the two empty folders.
  for (const name of ['Beta 2', 'Alpha']) {
    await expect(page.locator('.dfh-breadcrumb-current')).toHaveText(name);
    await page.locator('button', { hasText: 'Delete folder' }).click();
    await confirm(page);
    await expect(page.locator('.dfh-toast')).toContainText(`Deleted folder "${name}"`);
  }
  await expect(page.locator('.dfh-breadcrumb-current')).toHaveText(project);

  // Archive, restore and delete the now empty project from the Projects list inspector.
  await page.goto('/Projects');
  await page.locator('#projects-table tbody tr', { hasText: project }).click();
  await expect(page.locator('#dfh-inspector-body .dfh-insp-title')).toHaveText(project);
  await page.locator('#dfh-inspector-body button[aria-label="Archive"]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Project archived');
  await page.goto('/Projects?status=archived');
  await page.locator('#projects-table tbody tr', { hasText: project }).click();
  await page.locator('#dfh-inspector-body button[aria-label="Restore"]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Project restored');
  await expect(page.locator('#projects-table tbody tr', { hasText: project })).toHaveCount(0); // gone from the archived list
  await page.goto('/Projects');
  await page.locator('#projects-table tbody tr', { hasText: project }).click();
  await page.locator('#dfh-inspector-body button[aria-label="Delete"]').click();
  await confirm(page);
  await expect(page.locator('.dfh-toast')).toContainText('Project deleted');
  await expect(page.locator('#projects-table tbody tr', { hasText: project })).toHaveCount(0);
});

test('team: create, add a member, change the role, remove, rename, delete', async ({ page }) => {
  const team = `E2E team ${Date.now()}`;
  await page.goto('/Teams/Create');
  await page.locator('input[name="Name"]').fill(team);
  await page.locator('button', { hasText: 'Create team' }).click();
  await expect(page.locator('.dfh-toast')).toContainText(`Created team "${team}"`);

  await page.locator('.dfh-entity-header button', { hasText: 'Add member' }).click();
  await page.locator('#dfh-add-member-modal input[name="email"]').fill(DEMO_ADMIN.email);
  await page.locator('#dfh-add-member-modal button[type="submit"]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Added Demo Admin');

  const row = page.locator('.dfh-table--members tbody tr', { hasText: DEMO_ADMIN.email });
  await row.locator('select[name="newRole"]').selectOption('admin');
  await expect(page.locator('.dfh-toast')).toContainText('Role changed to Admin');
  await row.locator('button[aria-label^="Remove"]').click();
  await confirm(page);
  await expect(page.locator('.dfh-toast')).toContainText('Member removed');
  await expect(page.locator('.dfh-table--members tbody tr')).toHaveCount(1);

  await page.locator('.dfh-entity-header a', { hasText: 'Settings' }).click();
  await page.locator('input[name="Name"]').fill(`${team} renamed`);
  await page.locator('.dfh-form-body button[type="submit"]', { hasText: 'Save' }).click();
  await expect(page.locator('.dfh-toast')).toContainText('Team saved');
  await expect(page.locator('.dfh-entity-header h1')).toHaveText(`${team} renamed`);

  await page.locator('.dfh-entity-header a', { hasText: 'Settings' }).click();
  await page.locator('button', { hasText: 'Delete team' }).click();
  await confirm(page);
  await expect(page.locator('.dfh-toast')).toContainText(`Deleted team "${team} renamed"`);
  await expect(page).toHaveURL(/\/Teams$/);
});
