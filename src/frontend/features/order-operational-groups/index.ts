/**
 * Public boundary of the order operational-groups feature — the ONLY module the app imports.
 *
 * TB-254-R1: this feature owns the rendered use-case surface that was previously dropped as a loose
 * file directly under `app/admin`. It exposes the one Marketplace operational-group workflow dialog
 * and its narrow input contract (display facts + the display formatters the page already owns).
 *
 * The caller keeps ownership of state, transport, the durable occurrence envelope and every mutation;
 * this module is display + local review interaction only.
 */
export { AdminOperationalGroupWorkflowDialog } from "./ui/admin-operational-group-workflow-dialog.tsx";

// TB-256-R1: the group's customer-delivery vs planned-carrier-handoff rows (display-only).
export { OperationalDeliveryTimeline } from "./ui/operational-delivery-timeline.tsx";

export type {
  OperationalDeliveryTimelineProps,
  OperationalPlannedDispatchFact,
} from "./ui/operational-delivery-timeline.tsx";

export type {
  OperationalGroupDialogLine,
  OperationalGroupDialogParcel,
  OperationalGroupDialogRequest,
  OperationalGroupDialogRow,
  OperationalGroupWorkflowFormatters,
  OperationalGroupWorkflowGroup,
  RetainedOperationalGroupEnvelope,
} from "./ui/admin-operational-group-workflow-dialog.tsx";
