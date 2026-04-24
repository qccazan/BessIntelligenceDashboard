import type { Page } from '@playwright/test';

export async function login(page: Page) {
  await page.goto('/');
  await page.getByLabel('Username').fill('admin');
  await page.getByLabel('Password').fill('admin');
  await page.getByRole('button', { name: /log in/i }).click();
  await page.waitForURL('/dashboard');
}
