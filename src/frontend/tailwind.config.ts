import type { Config } from "tailwindcss";

/**
 * اسکن محدود قالب‌های app. توکن‌های Design System تجاری هنوز استخراج نشده‌اند.
 */
const config: Config = {
  content: ["./app/**/*.{ts,tsx}"],
  theme: {
    extend: {},
  },
  plugins: [],
};

export default config;
