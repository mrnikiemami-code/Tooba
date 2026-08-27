"use client";

import { SELLER_STORY_CAPABILITIES, StoryManagementScreen } from "../../stories/management";

/**
 * مدیریت استوری فروشنده — همان UI مشترک Admin با قابلیت‌های محدود Seller.
 */
export default function VendorStoriesPage() {
  return <StoryManagementScreen capabilities={SELLER_STORY_CAPABILITIES} />;
}
