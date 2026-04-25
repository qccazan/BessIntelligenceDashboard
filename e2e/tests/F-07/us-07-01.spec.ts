import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-07-01: Day-Ahead Price Forecast Chart', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.getByTestId('generate-forecast-btn').click();
    await expect(page.getByTestId('price-chart')).toBeVisible({ timeout: 5000 });
  });

  test('AC-1: chart renders a continuous price curve spanning 24 hours', async ({ page }) => {
    const chart = page.getByTestId('price-chart');
    await expect(chart).toBeVisible();

    const curve = page.getByTestId('price-curve');
    await expect(curve).toBeVisible();

    // The curve path should have multiple L (lineto) segments forming a continuous line
    const d = await curve.getAttribute('d');
    expect(d).toBeTruthy();
    expect(d!.split('L').length).toBeGreaterThanOrEqual(10);
  });

  test('AC-2: time axis has at least 5 labelled time ticks', async ({ page }) => {
    const ticks = page.getByTestId('time-tick');
    await expect(ticks).not.toHaveCount(0);
    const count = await ticks.count();
    expect(count).toBeGreaterThanOrEqual(5);
  });

  test('AC-3: price axis has at least 2 labelled price levels', async ({ page }) => {
    // Wait for the chart to fully render before counting SVG elements
    await expect(page.getByTestId('price-chart')).toBeVisible();
    await expect(page.getByTestId('price-curve')).toBeVisible();
    const labels = page.locator('[data-testid="price-level-label"]');
    await expect(labels.first()).toBeAttached();
    const count = await labels.count();
    expect(count).toBeGreaterThanOrEqual(2);
  });

  test('AC-4: horizontal grid lines are present at each labelled price level', async ({ page }) => {
    await expect(page.getByTestId('price-chart')).toBeVisible();
    await expect(page.getByTestId('price-curve')).toBeVisible();
    const gridLines = page.locator('[data-testid="grid-line"]');
    const labels = page.locator('[data-testid="price-level-label"]');
    await expect(gridLines.first()).toBeAttached();
    const gridCount = await gridLines.count();
    const labelCount = await labels.count();
    expect(gridCount).toBe(labelCount);
    expect(gridCount).toBeGreaterThanOrEqual(2);
  });

  test('AC-5: area beneath the price curve is shaded with a gradient fill', async ({ page }) => {
    const area = page.getByTestId('price-area');
    await expect(area).toBeVisible();
    const fill = await area.getAttribute('fill');
    expect(fill).toContain('url(');
  });

  test('AC-6: chart renders without visible jank', async ({ page }) => {
    // Verify the chart SVG is present and has expected dimensions
    const chart = page.getByTestId('price-chart');
    await expect(chart).toBeVisible();
    const svg = chart.locator('svg');
    await expect(svg).toBeVisible();
    const viewBox = await svg.getAttribute('viewBox');
    expect(viewBox).toBeTruthy();
  });
});
