import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

const API_URL = process.env.API_URL || 'http://127.0.0.1:5000';

test.describe('US-07-03: AI Portfolio Recommendation Block', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.getByTestId('generate-forecast-btn').click();
    await expect(page.getByTestId('recommendation-block')).toBeVisible({ timeout: 5000 });
  });

  test('AC-1: recommendation block is visible inside the Market Forecast card', async ({ page }) => {
    const card = page.getByTestId('market-forecast-card');
    await expect(card).toBeVisible();
    const block = page.getByTestId('recommendation-block');
    await expect(block).toBeVisible();
  });

  test('AC-2: Charge/Discharge action shows action verb, both window rows, and confidence badge', async ({ page }) => {
    const action = page.getByTestId('rec-action');
    await expect(action).toBeVisible();
    const actionText = await action.textContent();
    expect(actionText?.length).toBeGreaterThan(0);

    const chargeWindow = page.getByTestId('rec-charge-window');
    await expect(chargeWindow).toBeVisible();

    const dischargeWindow = page.getByTestId('rec-discharge-window');
    await expect(dischargeWindow).toBeVisible();

    const confidence = page.getByTestId('rec-confidence');
    await expect(confidence).toBeVisible();
  });

  test('AC-3: charge row displays correct window and price', async ({ page }) => {
    const chargeWindow = page.getByTestId('rec-charge-window');
    const text = await chargeWindow.textContent();
    expect(text).toBeTruthy();
    // Should contain time range pattern and €/MWh
    expect(text).toMatch(/\d{2}:\d{2}/);
    expect(text).toContain('€/MWh');
  });

  test('AC-4: discharge row displays correct window and price', async ({ page }) => {
    const dischargeWindow = page.getByTestId('rec-discharge-window');
    const text = await dischargeWindow.textContent();
    expect(text).toBeTruthy();
    expect(text).toMatch(/\d{2}:\d{2}/);
    expect(text).toContain('€/MWh');
  });

  test('AC-5: confidence percentage is displayed in a badge', async ({ page }) => {
    const confidence = page.getByTestId('rec-confidence');
    await expect(confidence).toBeVisible();
    const text = await confidence.textContent();
    expect(text).toMatch(/\d+%\s*confidence/);
  });

  test('AC-6: last-updated timestamp is visible', async ({ page }) => {
    const card = page.getByTestId('market-forecast-card');
    const meta = card.locator('p').filter({ hasText: /Updated \d{2}:\d{2}/ });
    await expect(meta).toBeVisible();
  });

  test('AC-7: AI active badge is visible regardless of action type', async ({ page }) => {
    const badge = page.getByTestId('ai-active-badge');
    await expect(badge).toBeVisible();
    await expect(badge).toHaveText('AI active');
  });

  test('AC-8: Hold action hides window rows and shows hold message', async ({ page }) => {
    // Set up API interception to return Hold action, then re-login
    // (page.reload() loses React auth state, so we must re-login)
    await page.route('**/api/recommendations/latest', async (route) => {
      const response = await route.fetch();
      const json = await response.json();
      json.portfolioAction = 'Hold';
      json.chargeWindowStart = '—';
      json.chargeWindowEnd = '—';
      json.dischargeWindowStart = '—';
      json.dischargeWindowEnd = '—';
      json.chargePrice = 0;
      json.dischargePrice = 0;
      await route.fulfill({ json });
    });

    await login(page);
    await page.getByTestId('generate-forecast-btn').click();
    await expect(page.getByTestId('recommendation-block')).toBeVisible({ timeout: 5000 });

    const holdMessage = page.getByTestId('hold-message');
    await expect(holdMessage).toBeVisible();
    await expect(holdMessage).toContainText('Insufficient price spread');

    // Charge and discharge window rows should not exist
    await expect(page.getByTestId('rec-charge-window')).not.toBeVisible();
    await expect(page.getByTestId('rec-discharge-window')).not.toBeVisible();
  });

  test('AC-9: Hold action still shows confidence badge and timestamp', async ({ page }) => {
    await page.route('**/api/recommendations/latest', async (route) => {
      const response = await route.fetch();
      const json = await response.json();
      json.portfolioAction = 'Hold';
      await route.fulfill({ json });
    });

    await login(page);
    await page.getByTestId('generate-forecast-btn').click();
    await expect(page.getByTestId('recommendation-block')).toBeVisible({ timeout: 5000 });

    const confidence = page.getByTestId('rec-confidence');
    await expect(confidence).toBeVisible();

    const card = page.getByTestId('market-forecast-card');
    const meta = card.locator('p').filter({ hasText: /Updated \d{2}:\d{2}/ });
    await expect(meta).toBeVisible();
  });
});
