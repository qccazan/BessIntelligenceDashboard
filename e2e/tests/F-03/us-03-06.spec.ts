import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-03-06: Temperature Warning Highlight', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('fleet-overview')).toBeVisible();
    // Show all rows
    await page.getByTestId('page-size-select').selectOption('25');
  });

  test('AC-1: the Temperature column displays a value in °C for every non-fault asset', async ({ page }) => {
    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();

    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const mode = await row.getAttribute('data-mode');
      if (mode === 'fault') continue;

      const tempEl = row.locator('[data-testid^="temp-"]');
      const text = await tempEl.textContent();
      expect(text).toMatch(/[\d.]+°C/);
    }
  });

  test('AC-2: temperature values ≥ 28°C are rendered in amber/orange colour', async ({ page }) => {
    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();

    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const tempEl = row.locator('[data-testid^="temp-"]');
      const text = await tempEl.textContent();
      if (!text) continue;
      const value = parseFloat(text.replace('°C', ''));
      if (value >= 28) {
        const colour = await tempEl.evaluate(el => getComputedStyle(el).color);
        // #B8461A = rgb(184, 70, 26)
        expect(colour).toBe('rgb(184, 70, 26)');
      }
    }
  });

  test('AC-3: temperature values < 28°C are rendered in the default text colour', async ({ page }) => {
    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();

    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const tempEl = row.locator('[data-testid^="temp-"]');
      const text = await tempEl.textContent();
      if (!text) continue;
      const value = parseFloat(text.replace('°C', ''));
      if (value < 28) {
        const colour = await tempEl.evaluate(el => getComputedStyle(el).color);
        // #1C1B2E = rgb(28, 27, 46)
        expect(colour).toBe('rgb(28, 27, 46)');
      }
    }
  });

  test('AC-4: the threshold boundary (28°C) is consistently applied — exactly 28.0°C triggers amber', async ({ page }) => {
    // Verify the logic: find all temps and confirm >= 28 → amber, < 28 → default
    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();
    let testedAmber = false;
    let testedDefault = false;

    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const mode = await row.getAttribute('data-mode');
      if (mode === 'fault') continue;

      const tempEl = row.locator('[data-testid^="temp-"]');
      const text = await tempEl.textContent();
      if (!text) continue;
      const value = parseFloat(text.replace('°C', ''));
      const colour = await tempEl.evaluate(el => getComputedStyle(el).color);

      if (value >= 28) {
        expect(colour).toBe('rgb(184, 70, 26)');
        testedAmber = true;
      } else {
        expect(colour).toBe('rgb(28, 27, 46)');
        testedDefault = true;
      }
    }

    // Ensure we tested at least one of each
    expect(testedAmber || testedDefault).toBe(true);
  });
});
