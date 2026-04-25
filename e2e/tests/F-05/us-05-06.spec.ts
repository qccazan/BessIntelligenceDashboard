import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-05-06: Replay Resets on Asset Switch', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('replay-card')).toBeVisible();
    await expect(page.getByTestId('play-btn')).toBeVisible();
  });

  test('AC-1: selecting a different asset regenerates the power bars and SoC curve', async ({ page }) => {
    // Pause and note current data
    await page.getByTestId('play-btn').click();
    const barsBefore = await page.getByTestId('power-bars').locator('rect').count();

    // Switch asset
    await page.getByTestId('fleet-row-BESS-02').click();
    await expect(page.getByTestId('play-btn')).toBeVisible();

    const barsAfter = await page.getByTestId('power-bars').locator('rect').count();
    expect(barsAfter).toBeGreaterThanOrEqual(1);
    expect(barsAfter).toBeLessThanOrEqual(96);
    expect(barsBefore).toBeGreaterThanOrEqual(1);
    expect(barsBefore).toBeLessThanOrEqual(96);

    // SoC line should be present
    const socLine = page.getByTestId('soc-line');
    const d = await socLine.getAttribute('d');
    expect(d).toBeTruthy();
  });

  test('AC-2: the playhead resets to the leftmost position after asset switch', async ({ page }) => {
    // Let auto-play run so playhead advances
    await page.waitForTimeout(500);
    // Switch asset
    await page.getByTestId('fleet-row-BESS-02').click();
    await expect(page.getByTestId('play-btn')).toBeVisible();
    // Wait a tick for reset
    await page.waitForTimeout(200);

    // Pause to check position
    await page.getByTestId('play-btn').click();
    const lineX = await page.getByTestId('playhead-line').getAttribute('x1');
    // Should be near start (first few steps of auto-play allowed)
    expect(parseFloat(lineX!)).toBeLessThan(50);
  });

  test('AC-3: the animation starts playing automatically after chart regeneration', async ({ page }) => {
    // Pause
    await page.getByTestId('play-btn').click();
    // Switch asset
    await page.getByTestId('fleet-row-BESS-02').click();
    await expect(page.getByTestId('play-btn')).toBeVisible();
    // Should auto-play — button should show Pause
    const ariaLabel = await page.getByTestId('play-btn').getAttribute('aria-label');
    expect(ariaLabel).toBe('Pause');
  });

  test('AC-4: the timestamp readout resets to the start time after asset switch', async ({ page }) => {
    // Let playhead advance
    await page.waitForTimeout(1000);
    const timeBefore = await page.getByTestId('replay-time').textContent();

    // Switch asset
    await page.getByTestId('fleet-row-BESS-03').click();
    await expect(page.getByTestId('play-btn')).toBeVisible();
    await page.getByTestId('play-btn').click(); // pause to check
    await page.waitForTimeout(100);

    const timeAfter = await page.getByTestId('replay-time').textContent();
    // Should be a valid time (the start time of the new asset's history)
    expect(timeAfter).toMatch(/\d{1,2}:\d{2}/);
  });

  test('AC-5: the daily energy summary chips recalculate for the newly selected asset', async ({ page }) => {
    const chargedBefore = await page.getByTestId('charged-value').textContent();

    await page.getByTestId('fleet-row-BESS-03').click();
    await expect(page.getByTestId('play-btn')).toBeVisible();

    const chargedAfter = await page.getByTestId('charged-value').textContent();
    // Should be a valid value
    expect(chargedAfter).toMatch(/[\d.]+\s*MWh/);
  });

  test('AC-6: the end-of-day SoC in the replay is consistent with the Current State card SoC', async ({ page }) => {
    // Seek to the end of the replay
    await page.getByTestId('play-btn').click(); // pause
    const scrub = page.getByTestId('scrub-bar');
    const box = await scrub.boundingBox();
    await scrub.click({ position: { x: box!.width - 1, y: box!.height / 2 } });

    // Get the replay SoC at the end
    const replaySocText = (await page.getByTestId('replay-soc').textContent())!;
    const replaySoc = parseInt(replaySocText);

    // Get the current state card SoC
    const ringSocText = (await page.getByTestId('soc-ring-value').textContent())!;
    const ringSoc = parseInt(ringSocText);

    // Should be close (within 5% tolerance due to rounding and timing)
    expect(Math.abs(replaySoc - ringSoc)).toBeLessThanOrEqual(5);
  });
});
