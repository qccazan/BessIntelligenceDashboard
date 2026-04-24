import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-03-03: Identify Fault Assets Immediately', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('fleet-overview')).toBeVisible();
    // Show all rows to ensure fault asset is visible
    await page.getByTestId('page-size-select').selectOption('25');
  });

  test('AC-1: fault-state assets render a pulsing/glowing animation on their row icon', async ({ page }) => {
    const faultRows = page.getByTestId('fleet-rows').locator('tr[data-mode="fault"]');
    const count = await faultRows.count();
    expect(count).toBeGreaterThanOrEqual(1);

    for (let i = 0; i < count; i++) {
      const icon = faultRows.nth(i).locator('[data-testid^="asset-icon-"]');
      const classes = await icon.getAttribute('class');
      expect(classes).toContain('animate-[faultPulse');
    }
  });

  test('AC-2: the Power cell for a fault asset displays "offline"', async ({ page }) => {
    const faultRows = page.getByTestId('fleet-rows').locator('tr[data-mode="fault"]');
    const count = await faultRows.count();
    expect(count).toBeGreaterThanOrEqual(1);

    for (let i = 0; i < count; i++) {
      const powerText = await faultRows.nth(i).locator('[data-testid^="power-"]').textContent();
      expect(powerText!.trim()).toBe('offline');
    }
  });

  test('AC-3: the Next Action cell for a fault asset displays "Hold" and "—"', async ({ page }) => {
    const faultRows = page.getByTestId('fleet-rows').locator('tr[data-mode="fault"]');
    const count = await faultRows.count();
    expect(count).toBeGreaterThanOrEqual(1);

    for (let i = 0; i < count; i++) {
      const actionText = await faultRows.nth(i).locator('[data-testid^="action-"]').first().textContent();
      expect(actionText!.trim()).toBe('Hold');
      const windowText = await faultRows.nth(i).locator('[data-testid^="action-window-"]').textContent();
      expect(windowText!.trim()).toBe('—');
    }
  });

  test('AC-4: the State badge for a fault asset is pink and labelled "Fault"', async ({ page }) => {
    const faultRows = page.getByTestId('fleet-rows').locator('tr[data-mode="fault"]');
    const count = await faultRows.count();
    expect(count).toBeGreaterThanOrEqual(1);

    for (let i = 0; i < count; i++) {
      const badge = faultRows.nth(i).locator('[data-testid^="state-badge-"]');
      const text = await badge.textContent();
      expect(text!.trim()).toBe('Fault');
      const bg = await badge.evaluate(el => getComputedStyle(el).backgroundColor);
      expect(bg).toBe('rgb(255, 228, 238)'); // #FFE4EE
    }
  });

  test('AC-5: the pulsing animation is continuous and does not stop', async ({ page }) => {
    const faultRows = page.getByTestId('fleet-rows').locator('tr[data-mode="fault"]');
    const icon = faultRows.first().locator('[data-testid^="asset-icon-"]');
    const classes = await icon.getAttribute('class');
    // The animation class includes 'infinite' keyword
    expect(classes).toContain('infinite');
  });
});
