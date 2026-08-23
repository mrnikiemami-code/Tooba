# TB-P04-T001 — Dependency inventory

From inspected `package.json` (not installed in reference tree).

Runtime: next 16.2.6, react 19.2.4, react-dom 19.2.4, axios, chart.js, framer-motion, fuse.js, lucide-react, next-themes, persian-date, persian-datepicker, react-chartjs-2, react-hook-form, @hookform/resolvers, react-loading-skeleton, react-otp-input, react-paginate, react-toastify, swiper, zod, zustand.

Dev: tailwindcss 4, @tailwindcss/postcss, eslint 9, eslint-config-next 16.2.6.

Classifications: see architecture document section "Dependency KEEP / REPLACE / REMOVE / DEFER map".

Reference `npm ci` / lint / build was **not** run: `node_modules` absent; installing would mutate an external tree and is not required to inspect source. Purchased source was not modified.
