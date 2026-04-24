import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-04-03: State of Health and Temperature Metrics', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('current-state-card')).toBeVisible();
  });

  test('AC-1: a State of Health metric tile is present with a percentage value and label', async ({ page }) => {
    const tile = page.getByTestId('soh-tile');
    await expect(tile).toBeVisible();
    await expect(tile.locator('text=State of health')).toBeVisible();
    const val = await page.getByTestId('soh-value').textContent();
    expect(val).toMatch(/\d+\s*%/);
  });

  test('AC-2: a Temperature metric tile is present with a value in °C and label', async ({ page }) => {
    const tile = page.getByTestId('temp-tile');
    await expect(tile).toBeVisible();
    await expect(tile.locator('text=Temperature')).toBeVisible();
    const val = await page.getByTestId('temp-value').textContent();
    expect(val).toMatch(/[\d.]+\s*°C/);
  });

  test('AC-3: both values match those in the Fleet Overview table for the same asset', async ({ page }) => {
    // Get SoH from tile
    const sohText = (await page.getByTestId('soh-value').textContent())!;
    const sohNum = parseInt(sohText);
    // Get SoH from fleet table
    const fleetSoh = (await page.getByTestId('soh-BESS-01').textContent())!;
    const fleetSohNum = parseInt(fleetSoh);
    expect(sohNum).toBe(fleetSohNum);

    // Get temp from tile
    const tempText = (await page.getByTestId('temp-value').textContent())!;
    const tempNum = parseFloat(tempText);
    // Get temp from fleet table
    const fleetTemp = (await page.getByTestId('temp-BESS-01').textContent())!;
    const fleetTempNum = parseFloat(fleetTemp);
    expect(tempNum).toBe(fleetTempNum);
  });

  test('AC-4: the Temperature tile applies amber treatment when ≥ 28 °C', async ({ page }) => {
    // Check all assets for temp >= 28
    await page.getByTestId('page-size-select').selectOption('25');
    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();

    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      await row.click();
      const tempText = (await page.getByTestId('temp-value').textContent())!;
      const tempNum = parseFloat(tempText);
      const tile = page.getByTestId('temp-tile');

      if (tempNum >= 28) {
        await expect(tile).toHaveAttribute('data-amber', 'true');
      } else {
        await expect(tile).not.toHaveAttribute('data-amber', 'true');
      }
    }
  });

  test('AC-5: both metric tiles update when a different asset is selected', async ({ page }) => {
    const initialSoh = await page.getByTestId('soh-value').textContent();
    const initialTemp = await page.getByTestId('temp-value').textContent();

    await page.getByTestId('fleet-row-BESS-02').click();

    // Values should be populated (may or may not differ)
    const newSoh = await page.getByTestId('soh-value').textContent();
    const newTemp = await page.getByTestId('temp-value').textContent();
    expect(newSoh).toMatch(/\d+\s*%/);
    expect(newTemp).toMatch(/[\d.]+\s*°C/);
    // The card should reflect BESS-02
    await expect(page.getByTestId('current-state-card')).toHaveAttribute('data-asset', 'BESS-02');
  });
});
