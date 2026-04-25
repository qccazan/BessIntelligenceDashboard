import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-07-02: Charge and Discharge Zone Overlays', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.getByTestId('generate-forecast-btn').click();
    await expect(page.getByTestId('recommendation-block')).toBeVisible({ timeout: 5000 });
  });

  test('AC-1: a hatched teal zone is overlaid in the low-price window', async ({ page }) => {
    const chargeZone = page.getByTestId('charge-zone');
    await expect(chargeZone).toBeVisible();
    const fill = await chargeZone.getAttribute('fill');
    expect(fill).toContain('url(');
  });

  test('AC-2: a hatched coral zone is overlaid in the high-price window', async ({ page }) => {
    const dischargeZone = page.getByTestId('discharge-zone');
    await expect(dischargeZone).toBeVisible();
    const fill = await dischargeZone.getAttribute('fill');
    expect(fill).toContain('url(');
  });

  test('AC-3: zones are labelled CHARGE and DISCHARGE', async ({ page }) => {
    const chargeLabel = page.getByTestId('charge-label');
    await expect(chargeLabel).toBeVisible();
    await expect(chargeLabel).toHaveText('CHARGE');

    const dischargeLabel = page.getByTestId('discharge-label');
    await expect(dischargeLabel).toBeVisible();
    await expect(dischargeLabel).toHaveText('DISCHARGE');
  });

  test('AC-4: teal dot marks minimum and coral dot marks maximum price', async ({ page }) => {
    const minDot = page.getByTestId('min-price-dot');
    await expect(minDot).toBeVisible();
    const minFill = await minDot.getAttribute('fill');
    expect(minFill).toBe('#17B890');

    const maxDot = page.getByTestId('max-price-dot');
    await expect(maxDot).toBeVisible();
    const maxFill = await maxDot.getAttribute('fill');
    expect(maxFill).toBe('#FF7A3D');
  });

  test('AC-5: charge zone aligns with price trough and discharge zone aligns with peak', async ({ page }) => {
    // Verify the charge zone and min dot share roughly the same horizontal area
    const chargeZone = page.getByTestId('charge-zone');
    const minDot = page.getByTestId('min-price-dot');
    const dischargeZone = page.getByTestId('discharge-zone');
    const maxDot = page.getByTestId('max-price-dot');

    const zoneX = parseFloat((await chargeZone.getAttribute('x')) ?? '0');
    const zoneW = parseFloat((await chargeZone.getAttribute('width')) ?? '0');
    const dotCx = parseFloat((await minDot.getAttribute('cx')) ?? '0');
    // Min dot should be within or near the charge zone (50px tolerance ≈ 1.5 hours)
    expect(dotCx).toBeGreaterThanOrEqual(zoneX - 50);
    expect(dotCx).toBeLessThanOrEqual(zoneX + zoneW + 50);

    const dZoneX = parseFloat((await dischargeZone.getAttribute('x')) ?? '0');
    const dZoneW = parseFloat((await dischargeZone.getAttribute('width')) ?? '0');
    const maxCx = parseFloat((await maxDot.getAttribute('cx')) ?? '0');
    expect(maxCx).toBeGreaterThanOrEqual(dZoneX - 50);
    expect(maxCx).toBeLessThanOrEqual(dZoneX + dZoneW + 50);
  });
});
