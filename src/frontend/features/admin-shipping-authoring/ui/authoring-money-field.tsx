"use client";

/**
 * Exact money TEXT field and its integer bound sibling.
 *
 * The displayed value is derived from the model token on every render, so grouping can never become part of
 * the committed value; an INVALID text is shown exactly as typed (never trimmed down to different digits)
 * and is refused by the row/amount validation, and the caret anchor keeps which side of the decimal
 * separator it is on, so typing `12`, `.`, `5` really yields `12.5`.
 *
 * A TYPED keystroke drops the field's own display separators before reading (`1,200000` cannot become an
 * invalid amount), while a real PASTE keeps the strict reader so `12,34` stays visibly invalid.
 *
 * Kept in its own module so the area/rate editors and the isolated row modal share ONE field without an
 * import cycle between them.
 */
import { useLayoutEffect, useRef } from "react";
import type { AuthoringMessages } from "../messages/authoring-messages.ts";
import {
  caretOffsetFor,
  displayMoneyText,
  interpretMoneyBackspace,
  interpretMoneyEdit,
  interpretMoneyTyping,
  isCanonicalMoneyText,
  moneyHasFraction,
  type MoneyCaret,
} from "../model/money-text.ts";

/** One integer text bound (weight/count/area range). */
export function BoundField({
  label,
  value,
  onChange,
  testId,
  inputMode,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  testId: string;
  inputMode?: "numeric" | "decimal";
  placeholder?: string;
}) {
  return (
    <label className="grid gap-0.5">
      <span className="text-xs font-bold text-gray-700">{label}</span>
      <input
        type="text"
        inputMode={inputMode}
        dir="ltr"
        className="min-h-9 w-full rounded-lg border border-border bg-surface px-2 text-sm text-gray-900"
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        data-testid={testId}
      />
    </label>
  );
}

/**
 * Exact money field: grouped DISPLAY, canonical token to the model, caret anchored to a digit position.
 *
 * @param policy Customer precision of the area's server-owned currency; under `integer` (IRR) a whole token
 *   shows no `.00` and a non-zero fraction is left visible for validation to refuse — never rounded.
 */
export function MoneyField({
  label,
  value,
  policy,
  messages,
  onChange,
  testId,
  placeholder,
}: {
  label: string;
  value: string;
  policy: "integer" | "scaled";
  messages: AuthoringMessages;
  /** Receives the CANONICAL ungrouped token, or the invalid text as typed. */
  onChange: (raw: string) => void;
  testId: string;
  placeholder?: string;
}) {
  const input = useRef<HTMLInputElement>(null);
  const pendingCaret = useRef<MoneyCaret | null>(null);
  // A token WITHOUT a separator is a whole rial amount, never a "visible fraction": only a real `.` that
  // carries a non-zero digit is one, so `1000` can never be reported as decimal.
  const fractionVisible = policy === "integer" && moneyHasFraction(value);
  const invalid = value !== "" && (!isCanonicalMoneyText(value) || fractionVisible);

  useLayoutEffect(() => {
    const element = input.current;
    const anchor = pendingCaret.current;
    pendingCaret.current = null;
    if (element === null || anchor === null) return;
    const caret = caretOffsetFor(element.value, anchor);
    element.setSelectionRange(caret, caret);
  });

  return (
    <label className="grid gap-0.5">
      <span className="text-xs font-bold text-gray-700">{label}</span>
      <input
        ref={input}
        type="text"
        inputMode={policy === "integer" ? "numeric" : "decimal"}
        dir="ltr"
        className="min-h-9 w-full rounded-lg border border-border bg-surface px-2 text-sm text-gray-900 aria-[invalid=true]:border-danger"
        value={displayMoneyText(value, policy)}
        placeholder={placeholder}
        data-testid={testId}
        aria-invalid={invalid}
        onChange={(event) => {
          const element = event.target;
          const caret = element.selectionStart ?? element.value.length;
          const pasted = (event.nativeEvent as InputEvent | undefined)?.inputType === "insertFromPaste";
          const edit = pasted ? interpretMoneyEdit(element.value, caret) : interpretMoneyTyping(element.value, caret);
          pendingCaret.current = edit.caret;
          onChange(edit.raw);
        }}
        onKeyDown={(event) => {
          if (event.key !== "Backspace" || event.ctrlKey || event.metaKey || event.altKey) return;
          const element = event.currentTarget;
          if (element.selectionStart !== element.selectionEnd) return;
          const edit = interpretMoneyBackspace(element.value, element.selectionStart ?? 0);
          if (edit === null) return;
          // Display grouping would restore the separator and swallow the key; the digit goes with it.
          event.preventDefault();
          pendingCaret.current = edit.caret;
          onChange(edit.raw);
        }}
      />
      {fractionVisible ? (
        <span className="text-[11px] font-semibold text-danger" data-testid={`${testId}-fraction-issue`}>
          {messages.rialFractionInvalid}
        </span>
      ) : null}
    </label>
  );
}
