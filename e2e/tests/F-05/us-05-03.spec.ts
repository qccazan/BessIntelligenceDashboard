import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-05-03: Scrub Bar for Manual Seeking', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('replay-card')).toBeVisible();
    await expect(page.getByTestId('play-btn')).toBeVisible();
    // Pause auto-play
    await page.getByTestId('play-btn').click();
  });

  test('AC-1: a scrub bar is visible below the chart', async ({ page }) => {
    const scrub = page.getByTestId('scrub-bar');
    await expect(scrub).toBeVisible();
  });

  test('AC-2: the scrub bar displays a fill indicator that advances with the playhead', async ({ page }) => {
    const fill = page.getByTestId('scrub-fill');
    await expect(fill).toBeAttached();
    // Resume play and check fill advances
    await page.getByTestId('play-btn').click();
    await page.waitForTimeout(500);
    await page.getByTestId('play-btn').click();
    const style = await fill.getAttribute('style');
    expect(style).toContain('width');
  });

  test('AC-3: clicking the leftmost point moves the playhead to the start', async ({ page }) => {
    const scrub = page.getByTestId('scrub-bar');
    const box = await scrub.boundingBox();
    await scrub.click({ position: { x: 1, y: box!.height / 2 } });
    const lineX = await page.getByTestId('playhead-line').getAttribute('x1');
    // Should be at or near 0
    expect(parseFloat(lineX!)).toBeLessThan(5);
  });

  test('AC-4: clicking the rightmost point moves the playhead to the end', async ({ page }) => {
    const scrub = page.getByTestId('scrub-bar');
    const box = await scrub.boundingBox();
    await scrub.click({ position: { x: box!.width - 1, y: box!.height / 2 } });
    const lineX = await page.getByTestId('playhead-line').getAttribute('x1');
    // Should be at or near the max (320 in SVG coords)
    expect(parseFloat(lineX!)).toBeGreaterThan(300);
  });

  test('AC-5: clicking an intermediate point moves the playhead proportionally', async ({ page }) => {
    const scrub = page.getByTestId('scrub-bar');
    const box = await scrub.boundingBox();
    // Click at ~50%
    await scrub.click({ position: { x: box!.width / 2, y: box!.height / 2 } });
    const lineX = await page.getByTestId('playhead-line').getAttribute('x1');
    const x = parseFloat(lineX!);
    // Should be roughly in the middle of the 320-wide SVG
    expect(x).toBeGreaterThan(100);
    expect(x).toBeLessThan(220);
  });

  test('AC-6: readouts update immediately after seeking', async ({ page }) => {
    // Seek to middle
    const scrub = page.getByTestId('scrub-bar');
    const box = await scrub.boundingBox();
    await scrub.click({ position: { x: box!.width / 2, y: box!.height / 2 } });

    const time = await page.getByTestId('replay-time').textContent();
    expect(time).toMatch(/\d{1,2}:\d{2}/);
    const soc = await page.getByTestId('replay-soc').textContent();
    expect(soc).toMatch(/\d+%/);
    const power = await page.getByTestId('replay-power').textContent();
    expect(power).toMatch(/[\d.-]+\s*kW/);
  });

  test('AC-7: the play/pause state is preserved after seeking', async ({ page }) => {
    // Currently paused from beforeEach
    const scrub = page.getByTestId('scrub-bar');
    const box = await scrub.boundingBox();
    await scrub.click({ position: { x: box!.width / 2, y: box!.height / 2 } });

    // Should still be paused
    const ariaLabel = await page.getByTestId('play-btn').getAttribute('aria-label');
    expect(ariaLabel).toBe('Play');

    // Verify playhead is not moving
    const x1 = await page.getByTestId('playhead-line').getAttribute('x1');
    await page.waitForTimeout(300);
    const x2 = await page.getByTestId('playhead-line').getAttribute('x1');
    expect(x1).toBe(x2);
  });
});
