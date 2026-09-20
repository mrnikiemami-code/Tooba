/**
 * Public boundary for admin payment receipts capability.
 */
export { AdminReceiptsScreen } from "./components/receipts-screen.tsx";
export {
  mapAdminReceipt,
  queryAdminReceiptsGrid,
  type AdminReceiptRow,
} from "./api/receipts-api.ts";
