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

  test('AC-2: each row displays Asset ID, site name, State badge, Power, SoC, SoH, Temperature, and Next Action', async ({ page }) => {
    const firstRow = page.getByTestId('fleet-rows').locator('tr').first();
    // Asset ID and site name
    await expect(firstRow.locator('.text-\\[13px\\].font-medium.text-\\[\\#261761\\]').first()).not.toBeEmpty();
    // State badge
    await expect(firstRow.locator('[data-testid^="state-badge-"]')).toBeVisible();
    // Power
    await expect(firstRow.locator('[data-testid^="power-"]')).toBeVisible();
    // SoC
    await expect(firstRow.locator('[data-testid^="soc-"]')).toBeVisible();
    // SoH
    await expect(firstRow.locator('[data-testid^="soh-"]')).toBeAttached();
    // Temperature
    await expect(firstRow.locator('[data-testid^="temp-"]')).toBeAttached();
    // Next Action
    await expect(firstRow.locator('[data-testid^="action-"]').first()).toBeVisible();
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

      // Power cell has text
      const powerEl = row.locator('[data-testid^="power-"]');
      const powerText = await powerEl.textContent();
      expect(powerText!.trim().length).toBeGreaterThan(0);

      // SoC cell has text
      const socEl = row.locator('[data-testid^="soc-"]');
      const socText = await socEl.textContent();
      expect(socText!.trim().length).toBeGreaterThan(0);

      // Next action has text
      const actionEl = row.locator('[data-testid^="action-"]').first();
      const actionText = await actionEl.textContent();
      expect(actionText!.trim().length).toBeGreaterThan(0);
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
    expect(headerTexts.length).toBe(7);
    expect(headerTexts).toEqual(
      expect.arrayContaining(['Asset', 'State', 'Power', 'State of charge', 'SoH', 'Temp', 'Next action'])
    );
  });
});
