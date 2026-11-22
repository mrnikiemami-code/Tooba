const { defineConfig } = require("@playwright/test");

module.exports = defineConfig({
  testDir: __dirname,
  timeout: 45000,
  expect: { timeout: 8000 },
  fullyParallel: false,
  workers: 1,
  reporter: [["list"]],
  use: {
    baseURL: process.env.TOOBA_FE || "http://127.0.0.1:3000",
    locale: "fa-IR",
    browserName: "chromium",
    headless: true,
    screenshot: "off",
    trace: "off",
    video: "off"
  }
});
