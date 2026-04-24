import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-07-05: Per-Battery Action Strip', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('AC-1: 12 total tiles are rendered in the per-battery strip', async ({ page }) => {
    const strip = page.getByTestId('per-battery-strip');
    await expect(strip).toBeVisible();

    const track = page.getByTestId('pb-track');
    const tiles = track.locator('[data-testid^="pb-tile-"]');
    await expect(tiles).toHaveCount(12);
  });

  test('AC-2: each tile displays asset ID, action verb, and time window', async ({ page }) => {
    const track = page.getByTestId('pb-track');
    const firstTile = track.locator('[data-testid^="pb-tile-"]').first();
    await expect(firstTile).toBeVisible();

    const tileText = await firstTile.textContent();
    expect(tileText).toBeTruthy();
    // Should contain a BESS code
    expect(tileText).toMatch(/BESS-\d{2}/);
    // Should contain an action
    expect(tileText).toMatch(/Charge|Discharge|Hold/);
  });

  test('AC-3: fault-state assets display Hold and — with no time window', async ({ page }) => {
    // Start fresh — navigate away so the dashboard unmounts
    await page.goto('/');

    // Set up API interception to inject a Hold battery action
    await page.route('**/api/recommendations/latest', async (route) => {
      const response = await route.fetch();
      const json = await response.json();
      if (json.batteryActions && json.batteryActions.length > 0) {
        const lastIdx = json.batteryActions.length - 1;
        json.batteryActions[lastIdx].action = 'Hold';
        json.batteryActions[lastIdx].windowStart = '—';
        json.batteryActions[lastIdx].windowEnd = '—';
        json.batteryActions[lastIdx].reason = 'Fault — held offline';
      }
      await route.fulfill({ json });
    });

    // Login triggers navigation to /dashboard, which mounts MarketForecastCard
    await login(page);

    const track = page.getByTestId('pb-track');
    const tiles = track.locator('[data-testid^="pb-tile-"]');
    await expect(tiles.first()).toBeVisible();
    const count = await tiles.count();

    let foundHold = false;
    for (let i = 0; i < count; i++) {
      const tile = tiles.nth(i);
      const action = await tile.getAttribute('data-action');
      if (action === 'hold') {
        foundHold = true;
        const text = await tile.textContent();
        expect(text).toContain('Hold');
        expect(text).toContain('—');
        break;
      }
    }
    expect(foundHold).toBe(true);
  });

  test('AC-4: tile backgrounds use correct colour scheme', async ({ page }) => {
    const track = page.getByTestId('pb-track');
    const tiles = track.locator('[data-testid^="pb-tile-"]');
    const count = await tiles.count();

    for (let i = 0; i < count; i++) {
      const tile = tiles.nth(i);
      const action = await tile.getAttribute('data-action');
      const classes = await tile.getAttribute('class');
      expect(classes).toBeTruthy();

      if (action === 'charge') {
        // Teal background
        expect(classes).toContain('bg-[rgba(221,247,238');
      } else if (action === 'discharge') {
        // Coral background
        expect(classes).toContain('bg-[rgba(255,232,222');
      }
      // Hold tiles have no special bg class
    }
  });

  test('AC-5: prev and next arrow buttons navigate through the 12 tiles', async ({ page }) => {
    const counter = page.getByTestId('pb-counter');
    await expect(counter).toBeVisible();
    const initialText = await counter.textContent();
    expect(initialText).toContain('1–');

    // Click next
    const nextBtn = page.getByLabel('Next');
    await nextBtn.click();

    const afterNext = await counter.textContent();
    // Counter should have changed (no longer starts with 1–)
    expect(afterNext).not.toBe(initialText);
  });

  test('AC-6: counter updates as the strip is navigated', async ({ page }) => {
    const counter = page.getByTestId('pb-counter');
    const initial = await counter.textContent();
    expect(initial).toMatch(/\d+–\d+ \/ 12/);

    await page.getByLabel('Next').click();
    const updated = await counter.textContent();
    expect(updated).toMatch(/\d+–\d+ \/ 12/);
    expect(updated).not.toBe(initial);
  });

  test('AC-7: clicking a tile updates selectedAssetId', async ({ page }) => {
    const track = page.getByTestId('pb-track');
    const secondTile = track.locator('[data-testid^="pb-tile-"]').nth(1);
    await secondTile.click();

    // The clicked tile should now have the selected border styling (border-2)
    const classes = await secondTile.getAttribute('class');
    expect(classes).toContain('border-2');
    expect(classes).toContain('border-[#7B5CF6]');
  });

  test('AC-8: number of visible tiles adjusts on narrower viewports', async ({ page }) => {
    // At full width, 5 tiles should be visible
    const counter = page.getByTestId('pb-counter');
    const fullText = await counter.textContent();
    expect(fullText).toContain('1–5');

    // Resize to narrow
    await page.setViewportSize({ width: 550, height: 900 });
    // Wait a moment for the resize handler
    await page.waitForTimeout(200);

    const narrowText = await counter.textContent();
    // Should show fewer tiles (1–3 or 1–4)
    expect(narrowText).toMatch(/1–[34] \/ 12/);
  });
});
