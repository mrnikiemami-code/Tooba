"use client";

/**
 * One short, actionable message on an EXPLICIT operator action.
 *
 * It reuses the existing design-system `Dialog` (one modal, one close action) instead of turning a
 * rejection into a permanent red paragraph next to the form, and it is never raised per keystroke: the
 * caller opens it only when the operator submitted something.
 */
import { Dialog } from "../../../design-system";

export function AuthoringMessageBox({
  open,
  title,
  text,
  closeLabel,
  testId,
  onClose,
}: {
  open: boolean;
  title: string;
  text: string;
  closeLabel: string;
  testId?: string;
  onClose: () => void;
}) {
  return (
    <Dialog open={open} onClose={onClose} title={title} showCloseButton={false}>
      <div className="space-y-3 text-sm text-gray-700" data-testid={testId}>
        <p>{text}</p>
        <div className="flex justify-end">
          <button
            type="button"
            className="inline-flex min-h-9 items-center rounded-lg border border-border bg-surface px-3 text-xs font-bold text-gray-800"
            data-testid={testId ? `${testId}-ok` : undefined}
            onClick={onClose}
          >
            {closeLabel}
          </button>
        </div>
      </div>
    </Dialog>
  );
}
