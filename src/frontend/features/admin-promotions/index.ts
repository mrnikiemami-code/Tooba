/**
 * Public boundary for admin promotions supervision capability.
 */
export { AdminPromotionsScreen } from "./components/promotions-screen.tsx";
export {
  loadAdminPromotions,
  deactivateAdminPromotion,
  mapAdminPromotions,
  type AdminPromotionRow,
} from "./api/promotions-api.ts";
