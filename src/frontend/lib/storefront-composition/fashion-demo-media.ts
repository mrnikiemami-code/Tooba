/**
 * Lightweight Fashion pilot media resolver — keep out of industry-template graph
 * so storefront-api does not pull composition seeds on every storefront page.
 */

const FASHION_IMAGES = [
  "https://images.unsplash.com/photo-1483985988355-763728e1935b?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1490481651871-ab68de25d43d?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1469334031218-e382a71b716b?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1445205170230-053b83016050?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1558769132-cb1aea458c5e?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1509631179647-0177331693ae?auto=format&fit=crop&w=800&q=80",
] as const;

export function fashionDemoMediaUrl(assetId: string | null | undefined): string | null {
  if (!assetId?.startsWith("demo-fashion-media-")) return null;
  const n = Number(assetId.replace("demo-fashion-media-", "")) || 1;
  return FASHION_IMAGES[(n - 1) % FASHION_IMAGES.length]!;
}

export { FASHION_IMAGES };
