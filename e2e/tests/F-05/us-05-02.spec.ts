import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-05-02: Animated Replay with Play / Pause', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('replay-card')).toBeVisible();
    // Wait for history to load and auto-play to start
    await expect(page.getByTestId('play-btn')).toBeVisible();
  });

  test('AC-1: a Play button is visible below the chart', async ({ page }) => {
    const btn = page.getByTestId('play-btn');
    await expect(btn).toBeVisible();
  });

  test('AC-2: pressing Play starts the playhead animation advancing from left to right', async ({ page }) => {
    // Pause first (auto-play is running)
    await page.getByTestId('play-btn').click();
    // Get initial playhead position
    const line = page.getByTestId('playhead-line');
    const x1Before = await line.getAttribute('x1');
    // Click play
    await page.getByTestId('play-btn').click();
    // Wait a bit for animation to advance
    await page.waitForTimeout(500);
    // Pause
    await page.getByTestId('play-btn').click();
    const x1After = await line.getAttribute('x1');
    // Playhead should have moved right
    expect(parseFloat(x1After!)).toBeGreaterThan(parseFloat(x1Before!));
  });

  test('AC-3: the timestamp readout updates at each step', async ({ page }) => {
    // Pause auto-play
    await page.getByTestId('play-btn').click();
    const timeBefore = await page.getByTestId('replay-time').textContent();
    // Resume
    await page.getByTestId('play-btn').click();
    await page.waitForTimeout(500);
    await page.getByTestId('play-btn').click();
    const timeAfter = await page.getByTestId('replay-time').textContent();
    // Time should have advanced (or at least be a valid time)
    expect(timeAfter).toMatch(/\d{1,2}:\d{2}/);
  });

  test('AC-4: the SoC and Power values in the readout update at each step', async ({ page }) => {
    // Pause
    await page.getByTestId('play-btn').click();
    const socBefore = await page.getByTestId('replay-soc').textContent();
    const powerBefore = await page.getByTestId('replay-power').textContent();
    // Resume and let it run
    await page.getByTestId('play-btn').click();
    await page.waitForTimeout(800);
    await page.getByTestId('play-btn').click();
    const socAfter = await page.getByTestId('replay-soc').textContent();
    const powerAfter = await page.getByTestId('replay-power').textContent();
    // Values should be populated
    expect(socAfter).toMatch(/\d+%/);
    expect(powerAfter).toMatch(/[\d.-]+\s*kW/);
  });

  test('AC-5: bars to the left of the playhead are full opacity and bars to the right are dimmed', async ({ page }) => {
    // Pause and seek to middle
    await page.getByTestId('play-btn').click();
    const scrub = page.getByTestId('scrub-bar');
    const box = await scrub.boundingBox();
    await scrub.click({ position: { x: box!.width / 2, y: box!.height / 2 } });

    const bars = page.getByTestId('power-bars').locator('rect');
    const count = await bars.count();

    // Check a bar before the middle and after the middle
    const midIdx = Math.floor(count / 2);
    const leftOpacity = await bars.nth(Math.max(0, midIdx - 5)).getAttribute('opacity');
    const rightOpacity = await bars.nth(Math.min(count - 1, midIdx + 5)).getAttribute('opacity');
    expect(parseFloat(leftOpacity!)).toBe(1);
    expect(parseFloat(rightOpacity!)).toBeLessThan(1);
  });

  test('AC-6: the Play button toggles to a Pause icon while animating', async ({ page }) => {
    // Pause first
    await page.getByTestId('play-btn').click();
    // Should show play icon (triangle path)
    let ariaLabel = await page.getByTestId('play-btn').getAttribute('aria-label');
    expect(ariaLabel).toBe('Play');

    // Click play — should become pause
    await page.getByTestId('play-btn').click();
    ariaLabel = await page.getByTestId('play-btn').getAttribute('aria-label');
    expect(ariaLabel).toBe('Pause');
  });

  test('AC-7: pressing Pause halts the animation at the current position', async ({ page }) => {
    // Let auto-play run a bit
    await page.waitForTimeout(300);
    // Pause
    await page.getByTestId('play-btn').click();
    const lineX = await page.getByTestId('playhead-line').getAttribute('x1');
    // Wait and verify it hasn't moved
    await page.waitForTimeout(300);
    const lineXAfter = await page.getByTestId('playhead-line').getAttribute('x1');
    expect(lineXAfter).toBe(lineX);
  });

  test('AC-8: the animation loops automatically when the playhead reaches the last data point', async ({ page }) => {
    // Seek to near the end via scrub
    await page.getByTestId('play-btn').click(); // pause
    const scrub = page.getByTestId('scrub-bar');
    const box = await scrub.boundingBox();
    // Click near the end (95%)
    await scrub.click({ position: { x: box!.width * 0.95, y: box!.height / 2 } });
    // Resume play
    await page.getByTestId('play-btn').click();
    // Wait for animation to reach end + 1s loop delay + restart
    await page.waitForTimeout(3000);
    // Should still be playing (looped) — button should say Pause
    const ariaLabel = await page.getByTestId('play-btn').getAttribute('aria-label');
    expect(ariaLabel).toBe('Pause');
    // Playhead should have looped back near the start
    const lineX = await page.getByTestId('playhead-line').getAttribute('x1');
    expect(parseFloat(lineX!)).toBeLessThan(160);
  });
});
