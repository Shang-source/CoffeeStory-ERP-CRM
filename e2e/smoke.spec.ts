import { expect, test } from '@playwright/test';

test('StoryCoffee app loads', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle(/StoryCoffee|Vite|React/);
});
