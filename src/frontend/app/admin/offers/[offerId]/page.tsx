"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { ErrorState } from "../../../../design-system";
import { loadAdminOfferDetail, type AdminOfferDetail } from "../../admin-offer-api";

export default function AdminOfferDetailPage() {
  const params = useParams<{ offerId: string }>();
  const [detail, setDetail] = useState<AdminOfferDetail | null>(null);
  const [error, setError] = useState<string | undefined>(undefined);

  useEffect(() => {
    if (!params?.offerId) return;
    void loadAdminOfferDetail(params.offerId).then((result) => {
      if (result.ok) setDetail(result.detail);
      else setError(result.errorCode);
    });
  }, [params?.offerId]);

  if (error) {
    return <ErrorState title="پیشنهاد خوانده نشد" detail={error} onRetry={() => window.location.reload()} retryLabel="تلاش دوباره" />;
  }
  return (
    <main className="space-y-4">
      <Link className="text-sm text-[#2563EB]" href="/admin/offers">بازگشت به پیشنهادها</Link>
      <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm" data-testid="admin-offer-detail">
        {!detail ? (
          <p className="text-sm text-gray-500">در حال بارگذاری…</p>
        ) : (
          <dl className="grid gap-4 text-sm sm:grid-cols-2">
            <div><dt className="text-gray-500">شناسهٔ پیشنهاد</dt><dd className="mt-1 font-mono text-xs" data-testid="admin-offer-detail-id">{detail.offerId}</dd></div>
            <div><dt className="text-gray-500">فروشنده</dt><dd className="mt-1 font-medium">{detail.sellerDisplayName || detail.sellerPartyId}</dd></div>
            <div><dt className="text-gray-500">محصول</dt><dd className="mt-1 font-medium">{detail.productTitle}</dd></div>
            <div><dt className="text-gray-500">SKU</dt><dd className="mt-1" dir="ltr">{detail.sellerSku ?? "—"}</dd></div>
            <div><dt className="text-gray-500">وضعیت</dt><dd className="mt-1">{detail.status === "Draft" ? "پیش‌نویس" : detail.status}</dd></div>
            <div><dt className="text-gray-500">قیمت</dt><dd className="mt-1 tabular-nums">{detail.amount == null ? "—" : detail.amount.toLocaleString("fa-IR")}</dd></div>
            <div><dt className="text-gray-500">موجودی</dt><dd className="mt-1 tabular-nums">{detail.onHand.toLocaleString("fa-IR")}</dd></div>
            <div><dt className="text-gray-500">قابل فروش</dt><dd className="mt-1 tabular-nums">{detail.availableUnits.toLocaleString("fa-IR")}</dd></div>
          </dl>
        )}
      </section>
    </main>
  );
}
