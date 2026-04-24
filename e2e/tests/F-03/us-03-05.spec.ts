import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-03-05: Paginate Through the Fleet', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('fleet-overview')).toBeVisible();
  });

  test('AC-1: by default, 5 rows are shown per page on initial load', async ({ page }) => {
    const rows = page.getByTestId('fleet-rows').locator('tr');
    await expect(rows).toHaveCount(5);
  });

  test('AC-2: a page-size selector offers options: 5, 10, 25 rows per page', async ({ page }) => {
    const select = page.locator('[data-testid="page-size-select"]');
    await expect(select).toBeVisible();
    const options = select.locator('option');
    await expect(options).toHaveCount(3);
    const values = await options.evaluateAll(opts =>
      opts.map(o => (o as HTMLOptionElement).value)
    );
    expect(values).toEqual(['5', '10', '25']);
  });

  test('AC-3: changing the page size updates the visible rows immediately', async ({ page }) => {
    // Default: 5 rows
    await expect(page.getByTestId('fleet-rows').locator('tr')).toHaveCount(5);

    // Change to 10
    await page.getByTestId('page-size-select').selectOption('10');
    await expect(page.getByTestId('fleet-rows').locator('tr')).toHaveCount(10);

    // Change to 25 — only 12 assets total
    await page.getByTestId('page-size-select').selectOption('25');
    await expect(page.getByTestId('fleet-rows').locator('tr')).toHaveCount(12);
  });

  test('AC-4: "Showing X–Y of Z assets" counter updates correctly', async ({ page }) => {
    const info = page.getByTestId('page-info');
    await expect(info).toContainText('1–5');
    await expect(info).toContainText('12');

    // Go to page 2
    await page.getByTestId('page-btn-2').click();
    await expect(info).toContainText('6–10');
    await expect(info).toContainText('12');

    // Go to page 3
    await page.getByTestId('page-btn-3').click();
    await expect(info).toContainText('11–12');
    await expect(info).toContainText('12');
  });

  test('AC-5: prev button is disabled on page 1, next button is disabled on the last page', async ({ page }) => {
    // Page 1: prev disabled, next enabled
    await expect(page.getByTestId('page-prev')).toBeDisabled();
    await expect(page.getByTestId('page-next')).toBeEnabled();

    // Navigate to last page
    await page.getByTestId('page-btn-3').click();
    await expect(page.getByTestId('page-prev')).toBeEnabled();
    await expect(page.getByTestId('page-next')).toBeDisabled();
  });

  test('AC-6: numbered page buttons are present; the active page button is visually distinct', async ({ page }) => {
    // Page 1 button should be active
    const btn1 = page.getByTestId('page-btn-1');
    await expect(btn1).toHaveAttribute('data-active', 'true');

    // Page 2 should not be active
    const btn2 = page.getByTestId('page-btn-2');
    const isActive = await btn2.getAttribute('data-active');
    expect(isActive).toBeNull();

    // Click page 2 — it should become active, page 1 not
    await btn2.click();
    await expect(btn2).toHaveAttribute('data-active', 'true');
    const btn1Active = await btn1.getAttribute('data-active');
    expect(btn1Active).toBeNull();
  });

  test('AC-8: navigating to a page that contains the selected asset preserves the row highlight', async ({ page }) => {
    // Select BESS-01 (default)
    await expect(page.getByTestId('fleet-row-BESS-01')).toHaveAttribute('data-selected', 'true');

    // Navigate to page 2
    await page.getByTestId('page-btn-2').click();
    // BESS-01 is not on page 2, so no selected row visible

    // Navigate back to page 1
    await page.getByTestId('page-btn-1').click();
    // BESS-01 should still be highlighted
    await expect(page.getByTestId('fleet-row-BESS-01')).toHaveAttribute('data-selected', 'true');
  });
});
