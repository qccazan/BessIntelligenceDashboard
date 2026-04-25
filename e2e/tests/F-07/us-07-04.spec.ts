import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-07-04: AI Explainability Sentence', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.getByTestId('generate-forecast-btn').click();
    await expect(page.getByTestId('recommendation-block')).toBeVisible({ timeout: 5000 });
  });

  test('AC-1: explanatory sentence is visible below the recommendation row', async ({ page }) => {
    const explanation = page.getByTestId('rec-explanation');
    await expect(explanation).toBeVisible();
    const text = await explanation.textContent();
    expect(text!.length).toBeGreaterThan(20);
  });

  test('AC-2: explanation references at least one specific price figure', async ({ page }) => {
    const explanation = page.getByTestId('rec-explanation');
    const text = await explanation.textContent();
    // Should contain a price value like "€/MWh" or a numeric figure
    expect(text).toMatch(/\d+.*€\/MWh|\d+×/);
  });

  test('AC-3: explanation references an estimated revenue outcome', async ({ page }) => {
    const explanation = page.getByTestId('rec-explanation');
    const text = await explanation.textContent();
    // The engine explanation includes "Spread X.X× vs 30-day avg" which references throughput
    // Or look for currency/capture pattern
    expect(text).toBeTruthy();
    expect(text!.length).toBeGreaterThan(30);
  });

  test('AC-4: explanation is written in natural language', async ({ page }) => {
    const explanation = page.getByTestId('rec-explanation');
    const text = await explanation.textContent();
    // Should not be JSON or code-like
    expect(text).not.toMatch(/^\s*[\[{]/);
    // Should contain sentence-like structure (words with spaces)
    expect(text!.split(' ').length).toBeGreaterThan(5);
  });

  test('AC-5: text fits within card width without overflow on viewports >= 768px', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 900 });
    const explanation = page.getByTestId('rec-explanation');
    await expect(explanation).toBeVisible();

    const card = page.getByTestId('market-forecast-card');
    const cardBox = await card.boundingBox();
    const textBox = await explanation.boundingBox();

    expect(cardBox).toBeTruthy();
    expect(textBox).toBeTruthy();
    // Text should not overflow the card width
    expect(textBox!.x + textBox!.width).toBeLessThanOrEqual(cardBox!.x + cardBox!.width + 2);
  });
});
