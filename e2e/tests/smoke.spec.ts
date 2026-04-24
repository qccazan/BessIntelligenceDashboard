import { test, expect } from '@playwright/test';

const API_URL = 'http://localhost:5000';

test.describe('Smoke: setup verification', () => {
  test('frontend loads and shows the login screen', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: /BESS Intelligence/i })).toBeVisible();
    await expect(page.getByLabel('Username')).toBeVisible();
    await expect(page.getByLabel('Password')).toBeVisible();
    await expect(page.getByRole('button', { name: /log in/i })).toBeVisible();
  });

  test('backend health endpoint returns ok', async ({ request }) => {
    const response = await request.get(`${API_URL}/api/health`);
    expect(response.ok()).toBeTruthy();
    const body = await response.json();
    expect(body.status).toBe('healthy');
    expect(body.timestamp).toBeTruthy();
  });
});
