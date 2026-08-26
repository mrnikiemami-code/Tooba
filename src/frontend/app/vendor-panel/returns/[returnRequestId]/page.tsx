"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { ErrorState, faWorkspaceMessages } from "../../../../design-system";
import {
  loadSellerReturnDetail,
  sellerApproveReturn,
  sellerRejectReturn,
  type ReturnSnapshot,
} from "../../../returns/return-api";
import { ReturnDetailCard } from "../../../returns/return-ui";
import { readSellerPartyId } from "../../seller-api";

/** جزئیات مرجوعی فروشنده با approve/reject. */
export default function SellerReturnDetailPage() {
  const params = useParams<{ returnRequestId: string }>();
  const returnRequestId = params.returnRequestId;
  const [snapshot, setSnapshot] = useState<ReturnSnapshot | null>(null);
  const [message, setMessage] = useState<string | undefined>();
  const [busy, setBusy] = useState(false);

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

  async function approve() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) return;
    setBusy(true);
    const result = await sellerApproveReturn(sellerPartyId, returnRequestId);
    setBusy(false);
    if (result.ok) setSnapshot(result.snapshot);
    else setMessage(result.errorCode);
  }

  async function reject() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) return;
    setBusy(true);
    const result = await sellerRejectReturn(sellerPartyId, returnRequestId);
    setBusy(false);
    if (result.ok) setSnapshot(result.snapshot);
    else setMessage(result.errorCode);
  }

  if (!snapshot) {
    return <ErrorState title="مرجوعی پیدا نشد" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />;
  }

  const canDecide = snapshot.status === "Requested";

  return (
    <main className="space-y-4">
      <Link href="/vendor-panel/returns" className="text-sm text-[#2563EB] hover:underline">بازگشت به فهرست</Link>
      <ReturnDetailCard snapshot={snapshot} />
      {canDecide ? (
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            disabled={busy}
            onClick={() => void approve()}
            className="rounded-xl px-4 py-2 text-sm font-bold bg-emerald-600 text-white hover:bg-emerald-700 transition-colors disabled:opacity-50"
          >
            تأیید و بازپرداخت
          </button>
          <button
            type="button"
            disabled={busy}
            onClick={() => void reject()}
            className="rounded-xl px-4 py-2 text-sm font-bold bg-red-600 text-white hover:bg-red-700 transition-colors disabled:opacity-50"
          >
            رد درخواست
          </button>
        </div>
      ) : null}
    </main>
  );
}
