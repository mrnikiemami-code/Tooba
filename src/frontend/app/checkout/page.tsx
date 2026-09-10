import { redirect } from "next/navigation";

/**
 * مسیر قدیمی /checkout به /shipping هدایت می‌شود (T002).
 */
export default function CheckoutRedirectPage() {
  redirect("/shipping");
}
