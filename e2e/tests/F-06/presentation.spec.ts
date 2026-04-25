import { test, expect } from '@playwright/test';

test.describe('Presentation layout', () => {
  test('stage is aligned to the top-left with no letterboxing', async ({ page }) => {
    await page.goto('/presentation/index.html');

    const stage = page.locator('#stage');
    await expect(stage).toBeVisible();

    const box = await stage.boundingBox();
    expect(box).not.toBeNull();

    // Stage should start at or very near the top-left corner (within 2px tolerance)
    expect(box!.x).toBeLessThanOrEqual(2);
    expect(box!.y).toBeLessThanOrEqual(2);

    // Stage should fill (nearly) the entire viewport
    const viewport = page.viewportSize()!;
    expect(box!.width).toBeGreaterThanOrEqual(viewport.width - 2);
    expect(box!.height).toBeGreaterThanOrEqual(viewport.height - 2);
  });
});
