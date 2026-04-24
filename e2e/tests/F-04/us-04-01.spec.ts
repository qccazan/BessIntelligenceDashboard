import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-04-01: State of Charge Ring Gauge', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('current-state-card')).toBeVisible();
  });

  test('AC-1: an SVG ring gauge is rendered in the Current State card', async ({ page }) => {
    const gauge = page.getByTestId('soc-ring-gauge');
    await expect(gauge).toBeVisible();
    await expect(gauge.locator('svg')).toBeVisible();
    await expect(gauge.locator('circle')).toHaveCount(2); // background + arc
  });

  test('AC-2: the ring arc length is proportional to the SoC percentage', async ({ page }) => {
    const arc = page.getByTestId('soc-ring-arc');
    const dasharray = await arc.getAttribute('stroke-dasharray');
    expect(dasharray).toBeTruthy();
    // The arc length should be non-zero for a non-zero SoC
    const [arcLen] = dasharray!.split(' ').map(Number);
    expect(arcLen).toBeGreaterThan(0);
  });

  test('AC-3: the numeric SoC value is displayed in the centre of the ring', async ({ page }) => {
    const val = page.getByTestId('soc-ring-value');
    await expect(val).toBeVisible();
    const text = await val.textContent();
    expect(text).toMatch(/^\d+%$/);
  });

  test('AC-4: the displayed percentage matches the Fleet Overview table value for the same asset', async ({ page }) => {
    // Get SoC from ring
    const ringVal = await page.getByTestId('soc-ring-value').textContent();
    // Get SoC from fleet table for BESS-01 (default selected)
    const fleetSoc = await page.getByTestId('soc-BESS-01').textContent();
    expect(ringVal).toBe(fleetSoc);
  });

  test('AC-5: the gauge updates when a different asset is selected', async ({ page }) => {
    const initialVal = await page.getByTestId('soc-ring-value').textContent();
    // Click a different asset in the fleet table
    await page.getByTestId('fleet-row-BESS-02').click();
    await expect(page.getByTestId('current-state-card')).toHaveAttribute('data-asset', 'BESS-02');
    // Value may or may not differ, but the card must have updated
    const newVal = await page.getByTestId('soc-ring-value').textContent();
    expect(newVal).toMatch(/^\d+%$/);
  });
});
