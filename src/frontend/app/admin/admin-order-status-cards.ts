/**
 * KPI badge resolvers for Admin order detail — order lifecycle vs payment lifecycle stay independent.
 */
import { formatAdminStatus } from "./admin-api.ts";

export type StatusBadge = { text: string; className: string };

export function orderLifecycleStatusSource(detail: { status: string }): string {
  const status = detail.status?.trim();
  return status || "Submitted";
}

export function paymentLifecycleStatusSource(detail: {
  paymentState: string;
  payment?: { status: string } | null;
}): string {
  const gateway = detail.payment?.status?.trim();
  if (gateway) return gateway;
  const paymentState = detail.paymentState?.trim();
  return paymentState || "PendingPayment";
}

export function orderStatusBadge(status: string): StatusBadge {
  if (status === "Paid" || status === "Fulfilled" || status === "Delivered" || status === "Completed") {
    return { text: formatAdminStatus(status), className: "bg-emerald-50 text-emerald-700" };
  }
  if (status === "Cancelled" || status === "Canceled" || status === "Rejected") {
    return { text: formatAdminStatus(status), className: "bg-red-50 text-red-700" };
  }
  if (
    status === "Submitted"
    || status === "ReservationRequested"
    || status === "Processing"
    || status === "InFulfillment"
    || status === "ReadyToShip"
    || status === "AwaitingShipment"
    || status === "Mixed"
  ) {
    return { text: formatAdminStatus(status), className: "bg-amber-50 text-amber-700" };
  }
  return { text: formatAdminStatus(status), className: "bg-gray-100 text-gray-700" };
}

export function paymentStatusBadge(status: string): StatusBadge {
  if (status === "Paid" || status === "Succeeded" || status === "Captured") {
    return { text: formatAdminStatus(status), className: "bg-emerald-50 text-emerald-700" };
  }
  if (status === "PendingPayment" || status === "Pending" || status === "Authorized") {
    return { text: formatAdminStatus(status), className: "bg-blue-50 text-blue-700" };
  }
  if (status === "Failed" || status === "Cancelled" || status === "Canceled" || status === "Expired") {
    return { text: formatAdminStatus(status), className: "bg-red-50 text-red-700" };
  }
  return { text: formatAdminStatus(status), className: "bg-gray-100 text-gray-700" };
}

export function resolveOrderStatusCard(detail: { status: string }): StatusBadge {
  return orderStatusBadge(orderLifecycleStatusSource(detail));
}

export function resolvePaymentStatusCard(detail: {
  paymentState: string;
  payment?: { status: string } | null;
}): StatusBadge {
  return paymentStatusBadge(paymentLifecycleStatusSource(detail));
}
