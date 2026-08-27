"use client";

import { useCallback, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import {
  TICKET_PRIORITIES,
  formatTicketStatus,
  loadAdminTicketDetail,
  patchAdminTicket,
  replyAdminTicket,
  type TicketSnapshot,
} from "../../../support/support-api.ts";
import { SupportTicketThread } from "../../../support/support-ui.tsx";

const ADMIN_STATUSES = [
  "Open",
  "InProgress",
  "WaitingForCustomer",
  "WaitingForSeller",
  "Resolved",
  "Closed",
] as const;

/** جزئیات تیکت Admin با کنترل وضعیت/اولویت و یادداشت داخلی. */
export default function AdminTicketDetailPage() {
  const params = useParams<{ id: string }>();
  const ticketId = params?.id ?? "";
  const [snapshot, setSnapshot] = useState<TicketSnapshot | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusDraft, setStatusDraft] = useState("");
  const [priorityDraft, setPriorityDraft] = useState("");
  const [assignDraft, setAssignDraft] = useState("");
  const [patchMessage, setPatchMessage] = useState<string | null>(null);

  const refresh = useCallback(() => {
    if (!ticketId) return;
    setLoading(true);
    setError(null);
    void loadAdminTicketDetail(ticketId).then((result) => {
      setLoading(false);
      if (result.state === "denied") {
        setError("دسترسی Admin مجاز نیست");
        setSnapshot(null);
        return;
      }
      if (result.state === "error") {
        setError(result.message ?? "خطا");
        setSnapshot(null);
        return;
      }
      setSnapshot(result.data);
      if (result.data) {
        setStatusDraft(result.data.status);
        setPriorityDraft(result.data.priority);
        setAssignDraft(result.data.assignedOperatorActorUserId ?? "");
      }
    });
  }, [ticketId]);

  useEffect(refresh, [refresh]);

  async function applyPatch() {
    setPatchMessage(null);
    const result = await patchAdminTicket(ticketId, {
      status: statusDraft || null,
      priority: priorityDraft || null,
      assignedOperatorActorUserId: assignDraft.trim() || null,
    });
    if (result.state !== "ok" || !result.data) {
      setPatchMessage(result.message ?? "به‌روزرسانی ناموفق");
      return;
    }
    setSnapshot(result.data);
    setPatchMessage("ذخیره شد");
  }

  return (
    <SupportTicketThread
      audience="admin"
      listHref="/admin/tickets"
      snapshot={snapshot}
      loading={loading}
      error={error}
      onReply={async (body, isInternalNote) => {
        const result = await replyAdminTicket(ticketId, {
          body,
          isInternalNote,
          idempotencyKey: crypto.randomUUID(),
        });
        if (result.state !== "ok" || !result.data) {
          return { ok: false, errorCode: result.message ?? "support.reply.rejected" };
        }
        setSnapshot(result.data);
        return { ok: true };
      }}
      adminControls={
        snapshot ? (
          <div className="mt-4 grid gap-3 sm:grid-cols-3 border-t border-gray-100 pt-4" data-testid="admin-ticket-controls">
            <label className="text-xs text-gray-600 flex flex-col gap-1">
              وضعیت
              <select
                value={statusDraft}
                onChange={(e) => setStatusDraft(e.target.value)}
                className="px-3 py-2 rounded-xl border border-gray-200 bg-gray-50 text-sm"
              >
                {ADMIN_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {formatTicketStatus(s)}
                  </option>
                ))}
              </select>
            </label>
            <label className="text-xs text-gray-600 flex flex-col gap-1">
              اولویت
              <select
                value={priorityDraft}
                onChange={(e) => setPriorityDraft(e.target.value)}
                className="px-3 py-2 rounded-xl border border-gray-200 bg-gray-50 text-sm"
              >
                {TICKET_PRIORITIES.map((p) => (
                  <option key={p.value} value={p.value}>
                    {p.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="text-xs text-gray-600 flex flex-col gap-1">
              ارجاع به اپراتور (Actor Id)
              <input
                value={assignDraft}
                onChange={(e) => setAssignDraft(e.target.value)}
                className="px-3 py-2 rounded-xl border border-gray-200 bg-gray-50 text-sm font-mono"
                dir="ltr"
                placeholder="optional guid"
              />
            </label>
            <div className="sm:col-span-3 flex items-center gap-3">
              <button
                type="button"
                onClick={() => void applyPatch()}
                className="px-4 py-2 rounded-xl bg-[#E53935] text-white text-xs font-bold hover:bg-[#c62828]"
              >
                ذخیره تغییرات
              </button>
              {patchMessage ? <span className="text-xs text-gray-500">{patchMessage}</span> : null}
            </div>
          </div>
        ) : null
      }
    />
  );
}
