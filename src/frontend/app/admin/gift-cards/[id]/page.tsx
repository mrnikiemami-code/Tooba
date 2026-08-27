"use client";

import { useParams } from "next/navigation";
import { AdminGiftCardDetailScreen } from "../../../wallet/wallet-ui.tsx";

/** جزئیات و ابطال کارت هدیه. */
export default function AdminGiftCardDetailPage() {
  const params = useParams<{ id: string }>();
  const cardId = params?.id ?? "";
  if (!cardId) {
    return (
      <p className="text-sm text-red-600" dir="rtl">
        شناسه کارت نامعتبر است.
      </p>
    );
  }
  return <AdminGiftCardDetailScreen cardId={cardId} />;
}
