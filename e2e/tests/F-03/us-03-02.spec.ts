import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-03-02: Identify Operational State at a Glance', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('fleet-overview')).toBeVisible();
    // Show all rows
    await page.getByTestId('page-size-select').selectOption('25');
  });

  test('AC-1: each row displays exactly one State badge with one of four values', async ({ page }) => {
    const validStates = ['Charging', 'Discharging', 'Idle', 'Fault'];
    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();

    for (let i = 0; i < count; i++) {
      const badges = rows.nth(i).locator('[data-testid^="state-badge-"]');
      await expect(badges).toHaveCount(1);
      const text = await badges.textContent();
      expect(validStates).toContain(text!.trim());
    }
  });

  test('AC-2: badge colours match the colour scheme: teal, coral, muted grey/purple, pink', async ({ page }) => {
    const colourMap: Record<string, string> = {
      Charging: 'rgb(221, 247, 238)',   // #DDF7EE teal bg
      Discharging: 'rgb(255, 232, 222)', // #FFE8DE coral bg
      Idle: 'rgb(232, 230, 242)',         // #E8E6F2 muted purple bg
      Fault: 'rgb(255, 228, 238)',        // #FFE4EE pink bg
    };

    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();

    for (let i = 0; i < count; i++) {
      const badge = rows.nth(i).locator('[data-testid^="state-badge-"]');
      const text = (await badge.textContent())!.trim();
      const bg = await badge.evaluate(el => getComputedStyle(el).backgroundColor);
      expect(bg).toBe(colourMap[text]);
    }
  });

  test('AC-3: the asset icon colour matches the State badge colour for that row', async ({ page }) => {
    const iconColourMap: Record<string, string> = {
      charging: 'rgb(23, 184, 144)',    // #17B890
      discharging: 'rgb(255, 122, 61)', // #FF7A3D
      idle: 'rgb(123, 92, 246)',        // #7B5CF6
      fault: 'rgb(240, 91, 138)',       // #F05B8A
    };

    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();

    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const mode = await row.getAttribute('data-mode');
      const icon = row.locator('[data-testid^="asset-icon-"]');
      const bg = await icon.evaluate(el => getComputedStyle(el).backgroundColor);
      expect(bg).toBe(iconColourMap[mode!]);
    }
  });

  test('AC-4: a legend at the bottom of the Fleet Overview card maps each colour to its state label', async ({ page }) => {
    const legend = page.getByTestId('fleet-legend');
    await expect(legend).toBeVisible();

    const legendText = await legend.textContent();
    expect(legendText).toContain('Charging');
    expect(legendText).toContain('Discharging');
    expect(legendText).toContain('Idle');
    expect(legendText).toContain('Fault');
  });

  test('AC-5: the State badge colour and the sign of the Power value are mutually consistent', async ({ page }) => {
    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();

    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const mode = await row.getAttribute('data-mode');
      const powerText = await row.locator('[data-testid^="power-"]').textContent();

      if (mode === 'charging') {
        // Power should be positive (shows +)
        expect(powerText).toMatch(/^\+/);
      } else if (mode === 'discharging') {
        // Power should be negative (shows -)
        expect(powerText).toMatch(/^[−-]/);
      } else if (mode === 'fault') {
        expect(powerText!.trim()).toBe('offline');
      } else if (mode === 'idle') {
        expect(powerText).toMatch(/0\.0 kW/);
      }
    }
  });
});
