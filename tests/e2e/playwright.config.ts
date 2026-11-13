import { defineConfig } from '@playwright/test';

// Runs against an already running app (local `dotnet run`, the demo container, or the live demo):
//   BASE_URL=http://localhost:5199 npx playwright test
export default defineConfig({
  testDir: '.',
  timeout: 60_000,
  fullyParallel: false,
  workers: 1,
  reporter: [['list']],
  outputDir: './test-results',
  use: {
    baseURL: process.env.BASE_URL ?? 'http://localhost:5199',
    viewport: { width: 1280, height: 800 },
    colorScheme: 'dark',
    trace: 'retain-on-failure',
  },
});
