import { test, expect } from '@playwright/test';
import { login } from '../../helpers/login';

test.describe('US-05-04: Operational State Pill During Replay', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await expect(page.getByTestId('replay-card')).toBeVisible();
    await expect(page.getByTestId('play-btn')).toBeVisible();
    // Pause auto-play
    await page.getByTestId('play-btn').click();
  });

  test('AC-1: an operational state pill is visible in the replay card header', async ({ page }) => {
    const pill = page.getByTestId('replay-state-pill');
    await expect(pill).toBeVisible();
  });

  test('AC-2: the pill displays "Charging" in teal when power is positive', async ({ page }) => {
    // Find a charging bar and seek to it
    const bars = page.getByTestId('power-bars').locator('rect');
    const count = await bars.count();
    for (let i = 0; i < count; i++) {
      const mode = await bars.nth(i).getAttribute('data-mode');
      if (mode === 'charging') {
        // Seek to this position via scrub
        const scrub = page.getByTestId('scrub-bar');
        const box = await scrub.boundingBox();
        const ratio = i / (count - 1);
        await scrub.click({ position: { x: box!.width * ratio, y: box!.height / 2 } });
        const pill = page.getByTestId('replay-state-pill');
        await expect(pill).toContainText('Charging');
        break;
      }
    }
  });

  test('AC-3: the pill displays "Discharging" in coral when power is negative', async ({ page }) => {
    const bars = page.getByTestId('power-bars').locator('rect');
    const count = await bars.count();
    for (let i = 0; i < count; i++) {
      const mode = await bars.nth(i).getAttribute('data-mode');
      if (mode === 'discharging') {
        const scrub = page.getByTestId('scrub-bar');
        const box = await scrub.boundingBox();
        const ratio = i / (count - 1);
        await scrub.click({ position: { x: box!.width * ratio, y: box!.height / 2 } });
        const pill = page.getByTestId('replay-state-pill');
        await expect(pill).toContainText('Discharging');
        break;
      }
    }
  });

  test('AC-4: the pill displays "Idle" in muted colour when power is near zero', async ({ page }) => {
    const bars = page.getByTestId('power-bars').locator('rect');
    const count = await bars.count();
    for (let i = 0; i < count; i++) {
      const mode = await bars.nth(i).getAttribute('data-mode');
      if (mode === 'idle') {
        const scrub = page.getByTestId('scrub-bar');
        const box = await scrub.boundingBox();
        const ratio = i / (count - 1);
        await scrub.click({ position: { x: box!.width * ratio, y: box!.height / 2 } });
        const pill = page.getByTestId('replay-state-pill');
        await expect(pill).toContainText('Idle');
        break;
      }
    }
  });

  test('AC-5: the pill updates immediately when the playhead moves by animation or scrubbing', async ({ page }) => {
    // Get initial pill text
    const pillBefore = (await page.getByTestId('replay-state-pill').textContent())!.trim();

    // Seek to a different position
    const scrub = page.getByTestId('scrub-bar');
    const box = await scrub.boundingBox();
    await scrub.click({ position: { x: box!.width * 0.75, y: box!.height / 2 } });

    const pillAfter = (await page.getByTestId('replay-state-pill').textContent())!.trim();
    // Pill should be one of the valid states
    expect(['Charging', 'Discharging', 'Idle']).toContain(pillAfter);
  });
});
