"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { ErrorState, faWorkspaceMessages } from "../../../../design-system";
import { loadSellerReturnDetail, type ReturnSnapshot } from "../../../returns/return-api";
import { ReturnDetailCard, ReturnReviewModal } from "../../../returns/return-ui";
import { readSellerPartyId } from "../../seller-api";

/** جزئیات مرجوعی فروشنده — card + ReturnReviewModal مطابق returnDetailModal Shopeiva. */
export default function SellerReturnDetailPage() {
  const params = useParams<{ returnRequestId: string }>();
  const returnRequestId = params.returnRequestId;
  const [snapshot, setSnapshot] = useState<ReturnSnapshot | null>(null);
  const [message, setMessage] = useState<string | undefined>();
  const [reviewOpen, setReviewOpen] = useState(false);

  const refresh = useCallback(() => {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setMessage("seller.identity.missing");
      return;
    }
    void loadSellerReturnDetail(sellerPartyId, returnRequestId).then((result) => {
      setSnapshot(result.snapshot);
      setMessage(result.message);
    });
  }, [returnRequestId]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  useEffect(() => {
    if (snapshot?.status === "Requested") {
      setReviewOpen(true);
    }
  }, [snapshot?.returnRequestId, snapshot?.status]);

  if (!snapshot) {
    return <ErrorState title="مرجوعی پیدا نشد" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />;
  }

  const canDecide = snapshot.status === "Requested";

  return (
    <main className="space-y-4">
      <Link href="/vendor-panel/returns" className="text-sm text-[#2563EB] hover:underline">بازگشت به فهرست</Link>
      <ReturnDetailCard snapshot={snapshot} />
      {canDecide ? (
        <button
          type="button"
          onClick={() => setReviewOpen(true)}
          className="rounded-xl px-4 py-2 text-sm font-bold bg-[#2563EB] text-white hover:bg-blue-700 transition-colors"
        >
          بررسی و تصمیم‌گیری
        </button>
      ) : null}
      <ReturnReviewModal
        open={reviewOpen && canDecide}
        snapshot={snapshot}
        onClose={() => setReviewOpen(false)}
        onUpdated={(updated) => {
          setSnapshot(updated);
          setReviewOpen(false);
        }}
      />
    </main>
  );
}
