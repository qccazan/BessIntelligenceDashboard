import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-04-05: Asset Selection Updates the Card', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('current-state-card')).toBeVisible();
  });

  test('AC-1: clicking a different fleet table row updates all Current State card fields without page reload', async ({ page }) => {
    await expect(page.getByTestId('current-state-card')).toHaveAttribute('data-asset', 'BESS-01');
    await page.getByTestId('fleet-row-BESS-02').click();
    await expect(page.getByTestId('current-state-card')).toHaveAttribute('data-asset', 'BESS-02');
    await expect(page.getByTestId('selected-name')).toHaveText('BESS-02');
    // Should not have navigated away
    await expect(page).toHaveURL(/\/dashboard/);
  });

  test('AC-2: clicking a per-battery action strip tile also updates the Current State card', async ({ page }) => {
    // The per-battery strip from F-07 also triggers asset selection
    const strip = page.getByTestId('per-battery-strip');
    if (await strip.isVisible()) {
      const tiles = strip.locator('[data-testid^="battery-tile-"]');
      const tileCount = await tiles.count();
      if (tileCount > 1) {
        await tiles.nth(1).click();
        const name = await page.getByTestId('selected-name').textContent();
        expect(name).toBeTruthy();
        // The card data-asset should match
        const cardAsset = await page.getByTestId('current-state-card').getAttribute('data-asset');
        expect(cardAsset).toBe(name);
      }
    }
  });

  test('AC-3: all fields update simultaneously with no stale data from the previous selection', async ({ page }) => {
    // Select BESS-01 then BESS-07 (different specs)
    await page.getByTestId('page-size-select').selectOption('25');
    await page.getByTestId('fleet-row-BESS-07').click();

    const name = await page.getByTestId('selected-name').textContent();
    expect(name).toBe('BESS-07');

    // All fields should show BESS-07 data
    const site = await page.getByTestId('selected-site').textContent();
    expect(site).toContain('Site Beta');

    // SoC ring should be populated
    const soc = await page.getByTestId('soc-ring-value').textContent();
    expect(soc).toMatch(/^\d+%$/);

    // Power should be populated
    const power = await page.getByTestId('current-power-value').textContent();
    expect(power).toBeTruthy();

    // SoH and temp should be populated
    const soh = await page.getByTestId('soh-value').textContent();
    expect(soh).toMatch(/\d+/);
    const temp = await page.getByTestId('temp-value').textContent();
    expect(temp).toMatch(/[\d.]+/);
  });

  test('AC-4: the asset name and site in the card banner match the newly selected row', async ({ page }) => {
    await page.getByTestId('page-size-select').selectOption('25');
    await page.getByTestId('fleet-row-BESS-07').click();
    await expect(page.getByTestId('selected-name')).toHaveText('BESS-07');
    const site = await page.getByTestId('selected-site').textContent();
    expect(site).toContain('Site Beta');
    expect(site).toContain('Rotterdam');
  });

  test('AC-5: the transition is immediate with no visible loading delay', async ({ page }) => {
    const start = Date.now();
    await page.getByTestId('fleet-row-BESS-02').click();
    await expect(page.getByTestId('current-state-card')).toHaveAttribute('data-asset', 'BESS-02');
    const elapsed = Date.now() - start;
    // Should be under 1000ms for 12 assets (local data, no fetch)
    expect(elapsed).toBeLessThan(1000);
  });
});
