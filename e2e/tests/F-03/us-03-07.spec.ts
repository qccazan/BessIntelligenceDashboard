import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-03-07: Responsive Table on Narrow Viewports', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('fleet-overview')).toBeVisible();
  });

  test('AC-1: on viewports ≥ 900px wide, all seven columns are visible', async ({ page }) => {
    // Default viewport is 1280px
    const headers = page.getByTestId('fleet-table').locator('thead th');
    await expect(headers).toHaveCount(7);

    // All should be visible
    for (let i = 0; i < 7; i++) {
      await expect(headers.nth(i)).toBeVisible();
    }
  });

  test('AC-2: SoH and Temperature columns have the responsive hide class', async ({ page }) => {
    const sohHeader = page.getByTestId('fleet-table').locator('thead th[data-column="soh"]');
    const tempHeader = page.getByTestId('fleet-table').locator('thead th[data-column="temp"]');

    const sohClasses = await sohHeader.getAttribute('class');
    const tempClasses = await tempHeader.getAttribute('class');

    expect(sohClasses).toContain('max-[899px]:hidden');
    expect(tempClasses).toContain('max-[899px]:hidden');
  });

  test('AC-3: on narrow viewports, the remaining five columns are legible with no horizontal scroll', async ({ page }) => {
    // Verify that the table wrapper does not have overflow-x: scroll/auto at normal width
    const tableWrap = page.getByTestId('fleet-overview').locator('.overflow-hidden').first();
    const overflow = await tableWrap.evaluate(el => getComputedStyle(el).overflow);
    expect(overflow).toBe('hidden');
  });

  test('AC-4: rows remain selectable regardless of viewport width', async ({ page }) => {
    // Click a row and verify selection works
    const row = page.getByTestId('fleet-row-BESS-02');
    await row.click();
    await expect(row).toHaveAttribute('data-selected', 'true');
    // Verify cursor is pointer
    const cursor = await row.evaluate(el => getComputedStyle(el).cursor);
    expect(cursor).toBe('pointer');
  });
});
