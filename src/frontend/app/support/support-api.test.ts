import assert from "node:assert/strict";
import test from "node:test";
import {
  formatTicketCategory,
  formatTicketPriority,
  formatTicketStatus,
  mapTicketList,
  mapTicketSnapshot,
} from "./support-api.ts";

test("mapTicketSnapshot maps camel and Pascal and strips nothing for admin-shaped payload", () => {
  const snapshot = mapTicketSnapshot({
    TicketId: "11111111-1111-1111-1111-111111111111",
    RequesterKind: "Customer",
    RequesterActorUserId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    Subject: "پرداخت ناموفق",
    Category: "Payment",
    Priority: "High",
    Status: "Open",
    CreatedAt: "2026-08-27T10:00:00Z",
    UpdatedAt: "2026-08-27T10:00:00Z",
    Messages: [
      {
        MessageId: "22222222-2222-2222-2222-222222222222",
        TicketId: "11111111-1111-1111-1111-111111111111",
        AuthorKind: "Customer",
        AuthorActorUserId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        Body: "سلام",
        CreatedAt: "2026-08-27T10:00:00Z",
        IsInternalNote: false,
      },
      {
        MessageId: "33333333-3333-3333-3333-333333333333",
        TicketId: "11111111-1111-1111-1111-111111111111",
        AuthorKind: "Admin",
        AuthorActorUserId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        Body: "یادداشت",
        CreatedAt: "2026-08-27T11:00:00Z",
        IsInternalNote: true,
      },
    ],
  });

  assert.ok(snapshot);
  assert.equal(snapshot!.ticketId, "11111111-1111-1111-1111-111111111111");
  assert.equal(snapshot!.subject, "پرداخت ناموفق");
  assert.equal(snapshot!.messages.length, 2);
  assert.equal(snapshot!.messages[1]?.isInternalNote, true);
});

test("mapTicketList accepts array or paged envelope", () => {
  const fromArray = mapTicketList([
    {
      ticketId: "11111111-1111-1111-1111-111111111111",
      subject: "a",
      category: "Order",
      priority: "Low",
      status: "Closed",
      requesterKind: "Seller",
      messageCount: 1,
      createdAt: "2026-08-27T10:00:00Z",
    },
  ]);
  assert.equal(fromArray.items.length, 1);
  assert.equal(fromArray.total, 1);

  const fromPage = mapTicketList({
    items: fromArray.items,
    total: 9,
    page: 2,
    pageSize: 5,
  });
  assert.equal(fromPage.total, 9);
  assert.equal(fromPage.page, 2);
});

test("FA labels cover Support domain enums", () => {
  assert.equal(formatTicketStatus("WaitingForCustomer"), "در انتظار مشتری");
  assert.equal(formatTicketPriority("High"), "بالا");
  assert.equal(formatTicketCategory("Return"), "مرجوعی");
});
