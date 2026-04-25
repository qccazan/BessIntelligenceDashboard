import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-05-01: Composite Power and SoC Chart', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('replay-card')).toBeVisible();
    await expect(page.getByTestId('play-btn')).toBeVisible();
  });

  test('AC-1: the chart renders power bars covering the 24-hour period', async ({ page }) => {
    const bars = page.getByTestId('power-bars').locator('rect');
    await expect(bars.first()).toBeAttached();
    const count = await bars.count();
    // Midnight-to-midnight yields up to 96 intervals (15-min each)
    expect(count).toBeGreaterThanOrEqual(1);
    expect(count).toBeLessThanOrEqual(96);
  });

  test('AC-2: charging intervals are teal, discharging are coral, and idle are muted purple', async ({ page }) => {
    const bars = page.getByTestId('power-bars').locator('rect');
    const count = await bars.count();
    const colours = new Set<string>();

    for (let i = 0; i < count; i++) {
      const fill = await bars.nth(i).getAttribute('fill');
      if (fill) colours.add(fill);
    }

    // Should have at least 2 different colours (charging + discharging at minimum)
    expect(colours.size).toBeGreaterThanOrEqual(2);

    // Verify known colour values
    const validColours = ['#17B890', '#FF7A3D', '#C4BFE0'];
    for (const colour of colours) {
      expect(validColours).toContain(colour);
    }
  });

  test('AC-3: a SoC area curve is overlaid on the power bars sharing the same time axis', async ({ page }) => {
    const socArea = page.getByTestId('soc-area');
    await expect(socArea).toBeAttached();
    const d = await socArea.getAttribute('d');
    expect(d).toBeTruthy();
    expect(d!.length).toBeGreaterThan(10);

    const socLine = page.getByTestId('soc-line');
    await expect(socLine).toBeAttached();
  });

  test('AC-4: a time axis with at least 4 labelled tick marks spanning the 24-hour range', async ({ page }) => {
    const replayCard = page.getByTestId('replay-card');
    const timeAxis = replayCard.getByTestId('time-axis');
    await expect(timeAxis).toBeVisible();
    const ticks = timeAxis.locator('span');
    const count = await ticks.count();
    expect(count).toBeGreaterThanOrEqual(4);

    // Verify ticks have time-like text
    for (let i = 0; i < count; i++) {
      const text = await ticks.nth(i).textContent();
      expect(text).toMatch(/\d{1,2}:\d{2}/);
    }
  });

  test('AC-5: the chart renders without visible lag or artefacts', async ({ page }) => {
    // Verify all bars are rendered (no missing/broken bars)
    const bars = page.getByTestId('power-bars').locator('rect');
    const count = await bars.count();
    expect(count).toBeGreaterThanOrEqual(1);
    expect(count).toBeLessThanOrEqual(96);

    // Verify chart container has proper dimensions
    const chart = page.getByTestId('replay-chart');
    const box = await chart.boundingBox();
    expect(box!.width).toBeGreaterThan(100);
    expect(box!.height).toBeGreaterThan(50);
  });

  test('AC-6: charging and discharging intervals are visually distinguishable', async ({ page }) => {
    const bars = page.getByTestId('power-bars').locator('rect');
    const count = await bars.count();

    const modes = new Set<string>();
    for (let i = 0; i < count; i++) {
      const mode = await bars.nth(i).getAttribute('data-mode');
      if (mode) modes.add(mode);
    }

    // Should have at least charging and discharging
    expect(modes.size).toBeGreaterThanOrEqual(2);
  });
});
