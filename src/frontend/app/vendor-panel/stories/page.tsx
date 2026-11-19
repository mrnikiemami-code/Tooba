"use client";

import { SELLER_STORY_CAPABILITIES } from "../../stories/management";
import { StoryManagementRoute } from "../../stories/management/StoryManagementRoute";

/**
 * مدیریت استوری فروشنده — همان UI مشترک Admin با قابلیت‌های محدود Seller.
 */
export default function VendorStoriesPage() {
  return <StoryManagementRoute capabilities={SELLER_STORY_CAPABILITIES} />;
}
