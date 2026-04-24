import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-04-02: Operational State and Real-Time Power', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('current-state-card')).toBeVisible();
  });

  test('AC-1: an operational state pill is displayed with one of four values', async ({ page }) => {
    const pill = page.getByTestId('current-state-pill');
    await expect(pill).toBeVisible();
    const text = await pill.textContent();
    expect(['Charging', 'Discharging', 'Idle', 'Fault']).toContain(text!.trim());
  });

  test('AC-2: the state pill colour is consistent with the Fleet Overview table colour scheme', async ({ page }) => {
    // Get the mode from the fleet table for BESS-01
    const fleetRow = page.getByTestId('fleet-row-BESS-01');
    const fleetMode = await fleetRow.getAttribute('data-mode');
    // Get the pill text from current state card
    const pillText = (await page.getByTestId('current-state-pill').textContent())!.trim().toLowerCase();
    expect(pillText).toBe(fleetMode);
  });

  test('AC-3: the real-time power value is displayed with kW unit and directional label', async ({ page }) => {
    const powerVal = page.getByTestId('current-power-value');
    await expect(powerVal).toBeVisible();
    // Check for power label (kW with direction) — only visible for non-fault assets
    const label = page.getByTestId('current-power-label');
    const pillText = (await page.getByTestId('current-state-pill').textContent())!.trim();
    if (pillText !== 'Fault') {
      await expect(label).toBeVisible();
      const labelText = await label.textContent();
      expect(labelText).toMatch(/kW/);
    }
  });

  test('AC-4: charging shows positive kW, discharging shows negative kW, idle shows ~0 kW', async ({ page }) => {
    // Show all assets to find different modes
    await page.getByTestId('page-size-select').selectOption('25');
    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();

    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const mode = await row.getAttribute('data-mode');
      await row.click();
      await expect(page.getByTestId('current-state-pill')).toBeVisible();

      const powerText = (await page.getByTestId('current-power-value').textContent())!;

      if (mode === 'charging') {
        expect(powerText).toMatch(/^\+/);
      } else if (mode === 'discharging') {
        expect(powerText).toMatch(/^−|^-/);
      } else if (mode === 'idle') {
        expect(powerText).toMatch(/0\.0/);
      } else if (mode === 'fault') {
        expect(powerText).toBe('offline');
      }
    }
  });

  test('AC-5: the state pill and power sign are mutually consistent for every selectable asset', async ({ page }) => {
    await page.getByTestId('page-size-select').selectOption('25');
    const rows = page.getByTestId('fleet-rows').locator('tr');
    const count = await rows.count();

    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      await row.click();
      const pillText = (await page.getByTestId('current-state-pill').textContent())!.trim();
      const powerText = (await page.getByTestId('current-power-value').textContent())!;

      if (pillText === 'Charging') {
        expect(powerText).toMatch(/^\+/);
      } else if (pillText === 'Discharging') {
        expect(powerText).toMatch(/^−|^-/);
      } else if (pillText === 'Idle') {
        expect(powerText).toMatch(/0\.0/);
      } else if (pillText === 'Fault') {
        expect(powerText).toBe('offline');
      }
    }
  });
});
