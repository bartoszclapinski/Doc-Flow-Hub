import { expect, test, Page } from '@playwright/test';
import { DEMO_ADMIN, DEMO_USER, signIn } from './helpers';

// Admin, settings and profile writes. Like the other *-actions specs they run only on a non-demo
// instance seeded with the demo data.

async function skipOnDemo(page: Page) {
  await page.goto('/Projects');
  test.skip((await page.locator('a', { hasText: 'New project' }).count()) === 0, 'writes are disabled in the demo');
}

async function search(page: Page, text: string) {
  await page.goto('/Admin/Users?q=' + encodeURIComponent(text));
}

test('admin: create a user, lock, bulk unlock, assign a role, edit, delete', async ({ page }) => {
  await signIn(page, DEMO_ADMIN);
  await skipOnDemo(page);
  const email = `e2e-${Date.now()}@example.test`;

  await page.goto('/Admin/Users/Create');
  await page.locator('input[name="Input.FirstName"]').fill('Eve');
  await page.locator('input[name="Input.LastName"]').fill('Tester');
  await page.locator('input[name="Input.Email"]').fill(email);
  await page.locator('input[name="Input.Password"]').fill('Abcdefg1!');
  await page.locator('input[name="Input.ConfirmPassword"]').fill('Abcdefg1!');
  await page.locator('button', { hasText: 'Create user' }).click();
  await expect(page.locator('.dfh-toast')).toContainText('created successfully');

  // Lock from the row menu.
  await search(page, email);
  const row = page.locator('tbody tr[data-id]', { hasText: email });
  await row.locator('[data-bs-toggle="dropdown"]').click();
  await row.locator('.dropdown-item', { hasText: 'Lock' }).click();
  await expect(page.locator('.dfh-toast')).toContainText('User locked');
  await expect(page.locator('tbody tr[data-id]', { hasText: email })).toContainText('Locked');

  // Unlock with the bulk bar.
  await page.locator('tbody tr[data-id]', { hasText: email }).locator('.dfh-row-check').check();
  await page.locator('[data-dfh-users-bulk="UnlockAccount"]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Unlocked 1 user');
  await expect(page.locator('tbody tr[data-id]', { hasText: email })).toContainText('Active');

  // Assign a role through the bulk dialog.
  await page.locator('tbody tr[data-id]', { hasText: email }).locator('.dfh-row-check').check();
  await page.locator('[data-dfh-users-bulk="assign-role"]').click();
  await page.locator('#dfh-users-dialog [data-dfh-users-input="role"]').selectOption('User');
  await page.locator('#dfh-users-dialog [data-dfh-users-ok]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Gave the role to 1 user');

  // Edit the name on the Edit page.
  await page.locator('tbody tr[data-id]', { hasText: email }).locator('a.dfh-cell-link').click();
  await page.locator('.dfh-entity-header a', { hasText: 'Edit' }).click();
  await page.locator('input[name="Input.FirstName"]').fill('Evelyn');
  await page.locator('.dfh-form-footer button[type="submit"]').click();
  await expect(page.locator('.dfh-toast')).toContainText('updated successfully');
  await expect(page.locator('.dfh-entity-header h1')).toContainText('Evelyn Tester');

  // Delete with the bulk bar and its confirmation.
  await search(page, email);
  await page.locator('tbody tr[data-id]', { hasText: email }).locator('.dfh-row-check').check();
  await page.locator('[data-dfh-users-bulk="Delete"]').click();
  await page.locator('#dfh-users-dialog [data-dfh-users-ok]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Deleted 1 user');
  await expect(page.locator('tbody tr[data-id]', { hasText: email })).toHaveCount(0);
});

test('admin: your own account is left out of a bulk lock', async ({ page }) => {
  await signIn(page, DEMO_ADMIN);
  await skipOnDemo(page);
  await search(page, DEMO_ADMIN.email);
  await page.locator('tbody tr[data-id]', { hasText: DEMO_ADMIN.email }).locator('.dfh-row-check').check();
  await page.locator('[data-dfh-users-bulk="lock"]').click();
  await page.locator('#dfh-users-dialog [data-dfh-users-ok]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Your own account was left out');
  await expect(page.locator('tbody tr[data-id]', { hasText: DEMO_ADMIN.email })).toContainText('Active');
});

test('admin: create, edit and delete a role', async ({ page }) => {
  await signIn(page, DEMO_ADMIN);
  await skipOnDemo(page);
  const role = `E2E role ${Date.now()}`;
  await page.goto('/Admin/Roles/Create');
  await page.locator('input[name="Input.Name"]').fill(role);
  await page.locator('button', { hasText: 'Create role' }).click();
  await expect(page.locator('.dfh-toast')).toContainText('created successfully');

  await page.locator('tbody tr', { hasText: role }).locator('a').click();
  await page.locator('.dfh-entity-header a', { hasText: 'Edit' }).click();
  const renamed = `${role} renamed`;
  await page.locator('input[name="Input.Name"]').fill(renamed);
  await page.locator('.dfh-form-footer button[type="submit"]').click();
  await expect(page.locator('.dfh-toast')).toContainText('updated successfully');
  await expect(page.locator('.dfh-entity-header h1')).toHaveText(renamed);

  await page.locator('.dfh-entity-header a', { hasText: 'Delete' }).click();
  await page.locator('input[name="Input.ConfirmationText"]').fill(renamed);
  await page.locator('input[name="Input.ConfirmUnderstanding"][type="checkbox"]').check();
  await page.locator('.dfh-form-footer button[type="submit"]').click();
  await expect(page.locator('.dfh-toast')).toContainText('permanently deleted');
  await expect(page.locator('tbody tr', { hasText: renamed })).toHaveCount(0);
});

test('admin: system settings save and reset a category', async ({ page }) => {
  await signIn(page, DEMO_ADMIN);
  await skipOnDemo(page);
  await page.goto('/Admin/Settings');
  const limit = page.locator('#set-AI-Usage-DailyLimit');
  await limit.fill('999');
  await page.locator('.dfh-form-footer button[type="submit"]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Settings updated');
  await expect(limit).toHaveValue('999');
  await page.locator('button', { hasText: 'Reset ai to defaults' }).click();
  await expect(page.locator('.dfh-toast')).toContainText('reset to default');
  await expect(limit).toHaveValue('1000');
});

test('user: profile, AI settings and a wrong current password', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await skipOnDemo(page);
  await page.goto('/Account/Manage');
  const save = page.locator('.dfh-form-footer button[type="submit"]');
  await expect(save).toBeDisabled();
  await page.locator('#bio').fill(`Bio from a test ${Date.now()}`);
  await expect(page.locator('[data-dfh-dirty-label]')).toBeVisible();
  await save.click();
  await expect(page.locator('.dfh-toast')).toContainText('Profile saved');

  await page.goto('/Settings/AI');
  const cache = page.locator('input[name="Input.EnableCaching"][type="checkbox"]');
  const wasOn = await cache.isChecked();
  await cache.setChecked(!wasOn);
  await page.locator('.dfh-form-footer button.btn-primary').click();
  await expect(page.locator('.dfh-toast')).toContainText('AI settings saved');
  await expect(page.locator('input[name="Input.EnableCaching"][type="checkbox"]')).toBeChecked({ checked: !wasOn });

  await page.goto('/Account/Manage/ChangePassword');
  await page.locator('#Input_CurrentPassword').fill('Wrong-password1!');
  await page.locator('#Input_NewPassword').fill('Abcdefg1!');
  await page.locator('#Input_ConfirmPassword').fill('Abcdefg1!');
  await page.locator('.dfh-form-footer button[type="submit"]').click();
  await expect(page.locator('.dfh-form-error')).toContainText('Incorrect password');
});
