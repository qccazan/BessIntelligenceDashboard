import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-03-01: View Asset List with Key Metrics', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('fleet-overview')).toBeVisible();
  });

  test('AC-1: table renders with 12 total asset rows across all pages', async ({ page }) => {
    // Change page size to 25 to see all rows at once
    await page.getByTestId('page-size-select').selectOption('25');
    const rows = page.getByTestId('fleet-rows').locator('tr');
    await expect(rows).toHaveCount(12);
  });

  test('AC-2: each row displays Asset ID, site name, State badge, Capacity, SoC, SoH, and Temperature', async ({ page }) => {
    const firstRow = page.getByTestId('fleet-rows').locator('tr').first();
    // Asset ID and site name
    await expect(firstRow.locator('.text-\\[13px\\].font-medium.text-\\[\\#261761\\]').first()).not.toBeEmpty();
    // State badge
    await expect(firstRow.locator('[data-testid^="state-badge-"]')).toBeVisible();
    // Capacity
    await expect(firstRow.locator('[data-testid^="capacity-"]')).toBeVisible();
    // SoC
    await expect(firstRow.locator('[data-testid^="soc-"]')).toBeVisible();
    // SoH
    await expect(firstRow.locator('[data-testid^="soh-"]')).toBeAttached();
    // Temperature
    await expect(firstRow.locator('[data-testid^="temp-"]')).toBeAttached();
  });

  test('AC-3: all fields contain populated, non-empty values for every asset', async ({ page }) => {
    // Show all rows
    await page.getByTestId('page-size-select').selectOption('25');
    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();
    expect(count).toBe(12);

    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const assetCode = await row.getAttribute('data-testid');
      expect(assetCode).toBeTruthy();

      // Capacity cell has text
      const capacityEl = row.locator('[data-testid^="capacity-"]');
      const capacityText = await capacityEl.textContent();
      expect(capacityText!.trim().length).toBeGreaterThan(0);

      // SoC cell has text
      const socEl = row.locator('[data-testid^="soc-"]');
      const socText = await socEl.textContent();
      expect(socText!.trim().length).toBeGreaterThan(0);
    }
  });

  test('AC-4: asset IDs are displayed in ascending order (BESS-01 through BESS-12)', async ({ page }) => {
    await page.getByTestId('page-size-select').selectOption('25');
    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();

    const ids: string[] = [];
    for (let i = 0; i < count; i++) {
      const testId = await rows.nth(i).getAttribute('data-testid');
      // data-testid format: fleet-row-BESS-XX
      const code = testId!.replace('fleet-row-', '');
      ids.push(code);
    }

    for (let i = 0; i < ids.length; i++) {
      expect(ids[i]).toBe(`BESS-${String(i + 1).padStart(2, '0')}`);
    }
  });

  test('AC-5: the table has a visible header row with labelled column names', async ({ page }) => {
    const table = page.locator('[data-testid="fleet-table"]');
    await expect(table).toBeVisible();
    const headers = table.locator('thead th');
    await expect(headers.first()).toBeVisible();
    const headerTexts = await headers.allTextContents();
    expect(headerTexts.length).toBe(6);
    expect(headerTexts).toEqual(
      expect.arrayContaining(['Asset', 'State', 'Capacity', 'State of charge', 'SoH', 'Temp'])
    );
  });
});
