/** لبهٔ پین‌شدهٔ AG Grid که در RTL/LTR همیشه «انتهای» گرید را نگه می‌دارد. */
export function pinnedGridEdge(direction: "rtl" | "ltr"): "left" | "right" {
  return direction === "rtl" ? "left" : "right";
}
