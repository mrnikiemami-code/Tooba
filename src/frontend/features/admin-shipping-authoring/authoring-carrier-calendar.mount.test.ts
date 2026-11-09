/**
 * TB-256 — the two INDEPENDENT method timing calendars, really rendered.
 *
 * Static structure via `renderToStaticMarkup`, behaviour via a jsdom + react-dom mount of the shipped
 * carrier editor. Nothing here edits a value itself: every change goes through the component's own
 * callback contract, which is what the screen wires into the ONE editor session patch.
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test features/admin-shipping-authoring/authoring-carrier-calendar.mount.test.ts
 */
import "../order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement, useState } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import test from "node:test";
import {
  click,
  ensureDom,
  flush,
  mount,
  q,
  qq,
  setInputValue,
} from "../order-carrier-execution/test-dom-harness.ts";
import { AuthoringCarrierWeekEditor } from "./ui/authoring-carrier-week-editor.tsx";
import { AuthoringWeekEditor } from "./ui/authoring-week-editor.tsx";
import { authoringMessages } from "./messages/authoring-messages.ts";
import type { AuthoringMessages } from "./messages/authoring-messages.ts";
import type { EditorCarrierSchedule } from "./model/editor-document.ts";
import {
  carrierWeekIssues,
  configureCarrierWeek,
  copyCarrierWeekFromCustomer,
  createCarrierWeekForm,
  withCarrierDayTime,
  withCarrierDayWorking,
  withCarrierPendingTimeZone,
  withCarrierTimeZone,
  type CarrierWeekFormState,
} from "./model/editor-carrier-week.ts";
import {
  createWeekForm,
  withDayTime,
  withDayWorking,
  withTimeZone,
  type WeekFormState,
} from "./model/editor-week.ts";

const MESSAGES: AuthoringMessages = authoringMessages("fa");
const NOOP = () => undefined;

// Static markup needs the same global React the jsdom mount installs (classic JSX transform).
ensureDom();

function carrierEditor(carrier: CarrierWeekFormState, overrides: Record<string, unknown> = {}) {
  return createElement(AuthoringCarrierWeekEditor, {
    schedule: null,
    carrier,
    issues: carrierWeekIssues(carrier),
    editable: true,
    locale: "fa",
    messages: MESSAGES,
    customerConfigured: true,
    onPendingTimeZone: NOOP,
    onConfigure: NOOP,
    onTimeZone: NOOP,
    onDayWorking: NOOP,
    onDayTime: NOOP,
    onCopyFromCustomer: NOOP,
    onReset: NOOP,
    ...overrides,
  });
}

const STORED_CARRIER: EditorCarrierSchedule = {
  timeZoneId: "Asia/Tehran",
  days: [
    { weekday: 1, isWorking: true, startTime: "08:00:00", endTime: "12:00:00" },
    { weekday: 2, isWorking: false, startTime: null, endTime: null },
    { weekday: 3, isWorking: false, startTime: null, endTime: null },
    { weekday: 4, isWorking: false, startTime: null, endTime: null },
    { weekday: 5, isWorking: false, startTime: null, endTime: null },
    { weekday: 6, isWorking: false, startTime: null, endTime: null },
    { weekday: 7, isWorking: false, startTime: null, endTime: null },
  ],
};

test("an unconfigured carrier calendar offers configure + one-time copy, and is honestly named", () => {
  const html = renderToStaticMarkup(carrierEditor(createCarrierWeekForm(null)));
  assert.match(html, /data-testid="shipping-authoring-carrier-week"/);
  assert.match(html, /data-testid="shipping-authoring-carrier-unconfigured"/);
  assert.match(html, /data-testid="shipping-authoring-carrier-configure"/);
  assert.match(html, /data-testid="shipping-authoring-carrier-copy"/);
  assert.match(html, /data-testid="shipping-authoring-carrier-configure"[^>]*disabled=""/);
  assert.doesNotMatch(html, /data-testid="shipping-authoring-carrier-days"/, "no invented week of hours");
});

test("the copy action is unavailable until the customer week is configured", async () => {
  const unavailable = renderToStaticMarkup(carrierEditor(createCarrierWeekForm(null), { customerConfigured: false }));
  assert.match(unavailable, /data-testid="shipping-authoring-carrier-copy"[^>]*disabled=""/);

  const available = renderToStaticMarkup(carrierEditor(createCarrierWeekForm(null)));
  assert.doesNotMatch(available, /data-testid="shipping-authoring-carrier-copy"[^>]*disabled=""/);

  let copied = 0;
  const mounted = await mount(carrierEditor(createCarrierWeekForm(null), { onCopyFromCustomer: () => (copied += 1) }));
  await click(q(mounted.container, '[data-testid="shipping-authoring-carrier-copy"]'));
  assert.equal(copied, 1, "the explicit copy is the operator's own action");
  await mounted.unmount();
});

test("a configured carrier calendar renders seven of ITS OWN days with open/close bounds", async () => {
  const carrier = createCarrierWeekForm(STORED_CARRIER);
  const mounted = await mount(carrierEditor(carrier));
  const rows = qq(mounted.container, '[data-testid^="shipping-authoring-carrier-day-"]');
  assert.equal(rows.length, 7);
  // The customer week is a different component with different ids: carrier rendering never emits it.
  assert.equal(qq(mounted.container, '[data-testid^="shipping-authoring-week-day-"]').length, 0);

  const mondayOpen = q(mounted.container, '[data-testid="shipping-authoring-carrier-open-1"]') as HTMLInputElement;
  assert.equal(mondayOpen.value, "08:00");
  assert.equal((q(mounted.container, '[data-testid="shipping-authoring-carrier-close-1"]') as HTMLInputElement).value, "12:00");
  // A day with no acceptance has no bound inputs at all (nothing is fabricated for it).
  assert.equal(mounted.container.querySelector('[data-testid="shipping-authoring-carrier-open-2"]'), null);
  await mounted.unmount();
});

test("editing carrier hours goes through the component's own callback contract", async () => {
  const calls: string[] = [];
  const carrier = createCarrierWeekForm(STORED_CARRIER);
  const mounted = await mount(
    carrierEditor(carrier, {
      onDayTime: (weekday: number, bound: string, value: string) => calls.push(`${weekday}:${bound}:${value}`),
      onDayWorking: (weekday: number, isWorking: boolean) => calls.push(`${weekday}:${isWorking ? "on" : "off"}`),
    }),
  );
  await setInputValue(
    q(mounted.container, '[data-testid="shipping-authoring-carrier-close-1"]'),
    "14:30",
  );
  assert.deepEqual(calls, ["1:closesAt:14:30"]);

  await click(q(mounted.container, '[data-testid="shipping-authoring-carrier-accepting-2"]'));
  assert.deepEqual(calls.slice(1), ["2:on"]);

  await click(q(mounted.container, '[data-testid="shipping-authoring-carrier-notaccepting-1"]'));
  assert.deepEqual(calls.slice(2), ["1:off"]);
  await mounted.unmount();
});

/**
 * The screen's real composition: TWO independent drafts in one screen, each edited by its own handlers.
 * Changing one calendar must leave the other's values byte-identical.
 */
function BothCalendars({ onSnapshot }: { onSnapshot: (customer: WeekFormState, carrier: CarrierWeekFormState) => void }) {
  const [customer, setCustomer] = useState<WeekFormState>(createWeekForm(STORED_CARRIER));
  const [carrier, setCarrier] = useState<CarrierWeekFormState>(createCarrierWeekForm(STORED_CARRIER));
  onSnapshot(customer, carrier);
  return createElement(
    "div",
    null,
    createElement(AuthoringWeekEditor, {
      schedule: STORED_CARRIER,
      week: customer,
      issues: [],
      editable: true,
      locale: "fa",
      messages: MESSAGES,
      onPendingTimeZone: NOOP,
      onConfigure: NOOP,
      onTimeZone: (value: string) => setCustomer((current) => withTimeZone(current, value)),
      onSlotDuration: NOOP,
      onDayWorking: (weekday: number, isWorking: boolean) =>
        setCustomer((current) => (current.kind === "configured" ? withDayWorking(current, weekday, isWorking) : current)),
      onDayTime: (weekday: number, bound: "startTime" | "endTime", value: string) =>
        setCustomer((current) => (current.kind === "configured" ? withDayTime(current, weekday, bound, value) : current)),
      onReset: NOOP,
    }),
    createElement(AuthoringCarrierWeekEditor, {
      schedule: STORED_CARRIER,
      carrier,
      issues: carrierWeekIssues(carrier),
      editable: true,
      locale: "fa",
      messages: MESSAGES,
      customerConfigured: true,
      onPendingTimeZone: NOOP,
      onConfigure: NOOP,
      onTimeZone: (value: string) => setCarrier((current) => withCarrierTimeZone(current, value)),
      onDayWorking: (weekday: number, isWorking: boolean) =>
        setCarrier((current) => withCarrierDayWorking(current, weekday, isWorking)),
      onDayTime: (weekday: number, bound: "opensAt" | "closesAt", value: string) =>
        setCarrier((current) => withCarrierDayTime(current, weekday, bound, value)),
      onCopyFromCustomer: () => setCarrier((current) => copyCarrierWeekFromCustomer(current, customer)),
      onReset: NOOP,
    }),
  );
}

test("editing the carrier calendar never changes the customer delivery week", async () => {
  let customer: WeekFormState = createWeekForm(null);
  let carrier: CarrierWeekFormState = createCarrierWeekForm(null);
  const mounted = await mount(
    createElement(BothCalendars, {
      onSnapshot: (nextCustomer, nextCarrier) => {
        customer = nextCustomer;
        carrier = nextCarrier;
      },
    }),
  );
  assert.equal(qq(mounted.container, '[data-testid^="shipping-authoring-week-day-"]').length, 7);
  assert.equal(qq(mounted.container, '[data-testid^="shipping-authoring-carrier-day-"]').length, 7);

  await setInputValue(q(mounted.container, '[data-testid="shipping-authoring-carrier-close-1"]'), "13:00");
  const carrierDay = carrier.kind === "configured" ? carrier.days.find((day) => day.weekday === 1)! : null;
  assert.equal(carrierDay?.closesAt, "13:00", "the carrier calendar took the edit");
  const customerDay = customer.kind === "configured" ? customer.days.find((day) => day.weekday === 1)! : null;
  assert.equal(customerDay?.endTime, "12:00:00", "the customer week is untouched");

  // And the reverse: a customer edit leaves the carrier values exactly as they were.
  await setInputValue(q(mounted.container, '[data-testid="shipping-authoring-week-end-1"]'), "16:00");
  const customerAfter = customer.kind === "configured" ? customer.days.find((day) => day.weekday === 1)! : null;
  assert.equal(customerAfter?.endTime, "16:00");
  const carrierAfter = carrier.kind === "configured" ? carrier.days.find((day) => day.weekday === 1)! : null;
  assert.equal(carrierAfter?.closesAt, "13:00", "the carrier calendar is not a mirror of the customer week");

  await flush();
  await mounted.unmount();
});

test("configure requires a real time zone; the carrier copy stays an explicit one-time action", async () => {
  let carrier: CarrierWeekFormState = createCarrierWeekForm(null);
  const mounted = await mount(
    createElement(AuthoringCarrierWeekEditor, {
      schedule: null,
      carrier,
      issues: carrierWeekIssues(carrier),
      editable: true,
      locale: "fa",
      messages: MESSAGES,
      customerConfigured: true,
      onPendingTimeZone: NOOP,
      onConfigure: NOOP,
      onTimeZone: NOOP,
      onDayWorking: NOOP,
      onDayTime: NOOP,
      onCopyFromCustomer: NOOP,
      onReset: NOOP,
    }),
  );
  const configure = q(mounted.container, '[data-testid="shipping-authoring-carrier-configure"]') as HTMLButtonElement;
  assert.equal(configure.disabled, true, "an empty pending time zone cannot be configured");

  await mounted.rerender(
    carrierEditor(withCarrierPendingTimeZone(carrier, "Asia/Tehran")),
  );
  assert.equal(
    (q(mounted.container, '[data-testid="shipping-authoring-carrier-configure"]') as HTMLButtonElement).disabled,
    false,
  );
  await mounted.unmount();

  // Configure is an explicit operator act: the state alone never produces a schedule.
  carrier = withCarrierPendingTimeZone(carrier, "Asia/Tehran");
  const configured = configureCarrierWeek(carrier);
  assert.equal(configured.kind, "configured");
  assert.equal(carrierWeekIssues(configured).length, 0);
});
