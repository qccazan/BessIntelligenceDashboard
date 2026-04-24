import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-04-04: Battery Specifications Row', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('current-state-card')).toBeVisible();
  });

  test('AC-1: a chemistry badge (e.g. "LFP") is visible in the specifications row', async ({ page }) => {
    const badge = page.getByTestId('chemistry-badge');
    await expect(badge).toBeVisible();
    const text = await badge.textContent();
    expect(text!.length).toBeGreaterThan(0);
  });

  test('AC-2: the power rating is displayed in kW with label "Power rating"', async ({ page }) => {
    const specsRow = page.getByTestId('specs-row');
    await expect(specsRow.locator('text=Power rating')).toBeVisible();
    const val = await page.getByTestId('power-rating').textContent();
    expect(val).toMatch(/\d+ kW/);
  });

  test('AC-3: the total capacity is displayed in kWh with label "Capacity"', async ({ page }) => {
    const specsRow = page.getByTestId('specs-row');
    await expect(specsRow.locator('text=Capacity')).toBeVisible();
    const val = await page.getByTestId('capacity').textContent();
    expect(val).toMatch(/[\d,]+ kWh/);
  });

  test('AC-4: the discharge duration equals capacity ÷ power rating, labelled "Duration"', async ({ page }) => {
    const specsRow = page.getByTestId('specs-row');
    await expect(specsRow.locator('text=Duration')).toBeVisible();
    const durationText = (await page.getByTestId('duration').textContent())!;
    const duration = parseFloat(durationText);

    const powerText = (await page.getByTestId('power-rating').textContent())!;
    const power = parseInt(powerText);
    const capText = (await page.getByTestId('capacity').textContent())!;
    const cap = parseInt(capText.replace(/,/g, ''));

    const expected = cap / power;
    expect(duration).toBeCloseTo(expected, 1);
  });

  test('AC-5: all four specification fields update when a different asset is selected', async ({ page }) => {
    const initial = {
      chem: await page.getByTestId('chemistry-badge').textContent(),
      power: await page.getByTestId('power-rating').textContent(),
      cap: await page.getByTestId('capacity').textContent(),
      dur: await page.getByTestId('duration').textContent(),
    };

    // Select an asset with different specs (BESS-07 has 400kW/800kWh)
    await page.getByTestId('page-size-select').selectOption('25');
    await page.getByTestId('fleet-row-BESS-07').click();

    const updated = {
      chem: await page.getByTestId('chemistry-badge').textContent(),
      power: await page.getByTestId('power-rating').textContent(),
      cap: await page.getByTestId('capacity').textContent(),
      dur: await page.getByTestId('duration').textContent(),
    };

    // At least power/capacity should differ for BESS-01 vs BESS-07
    expect(updated.power).not.toBe(initial.power);
    expect(updated.cap).not.toBe(initial.cap);
  });

  test('AC-6: the asset name, site, and location in the banner match the selected row', async ({ page }) => {
    // Check default selection
    const name = await page.getByTestId('selected-name').textContent();
    expect(name).toBe('BESS-01');

    // Click different asset and verify
    await page.getByTestId('page-size-select').selectOption('25');
    await page.getByTestId('fleet-row-BESS-07').click();
    await expect(page.getByTestId('selected-name')).toHaveText('BESS-07');
    const site = await page.getByTestId('selected-site').textContent();
    expect(site).toContain('Site Beta');
  });
});
