/**
 * Public boundary for admin reviews moderation capability.
 */
export { AdminReviewsScreen } from "./components/reviews-screen.tsx";
export {
  loadAdminReviews,
  moderateAdminReview,
  mapAdminReviews,
  queryAdminReviewsGrid,
  type AdminReviewRow,
  type AdminReviewsPage,
} from "./api/reviews-api.ts";
