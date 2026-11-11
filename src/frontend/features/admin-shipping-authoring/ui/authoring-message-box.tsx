"use client";

/**
 * One short, actionable message on an EXPLICIT operator action.
 *
 * TB-263: the single implementation now lives in the design system (`MessageBox`), so the order operations
 * surfaces and the shipping authoring surfaces reuse ONE blocking-message component instead of two copies.
 * This module keeps the feature-local name and props the authoring screens already use.
 */
import { MessageBox } from "../../../design-system";

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
    <MessageBox
      open={open}
      title={title}
      text={text}
      closeLabel={closeLabel}
      testId={testId}
      onClose={onClose}
    />
  );
}
