export type StoryManagementMode = "admin" | "seller";

export type StoryCapabilities = {
  mode: StoryManagementMode;
  canCreate: boolean;
  canEdit: boolean;
  canSubmit: boolean;
  canReview: boolean;
  canPublish: boolean;
  canSchedule: boolean;
  canDisable: boolean;
  showOrigin: boolean;
  showSellerOwner: boolean;
};

export const ADMIN_STORY_CAPABILITIES: StoryCapabilities = {
  mode: "admin",
  canCreate: true,
  canEdit: true,
  canSubmit: false,
  canReview: true,
  canPublish: true,
  canSchedule: true,
  canDisable: true,
  showOrigin: true,
  showSellerOwner: true,
};

export const SELLER_STORY_CAPABILITIES: StoryCapabilities = {
  mode: "seller",
  canCreate: true,
  canEdit: true,
  canSubmit: true,
  canReview: false,
  canPublish: false,
  canSchedule: false,
  canDisable: false,
  showOrigin: false,
  showSellerOwner: false,
};

/** آیا فروشنده می‌تواند این استوری را برای بازبینی ارسال کند؟ */
export function canSubmitStory(reviewStatus: string): boolean {
  return reviewStatus === "None" || reviewStatus === "Rejected";
}

/** آیا فروشنده می‌تواند آیتم/فیلدهای استوری را ویرایش کند؟ */
export function canEditSellerStory(reviewStatus: string): boolean {
  return reviewStatus === "None" || reviewStatus === "Rejected";
}
