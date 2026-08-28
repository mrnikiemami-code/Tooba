"use client";

import { createPortal } from "react-dom";
import { useEffect, useState, type ReactNode } from "react";

/** رندر فرزند خارج از درخت DOM والد — برای popoverهای شناور بدون clip. */
export function Portal({
  children,
  container,
}: {
  children: ReactNode;
  container?: HTMLElement | null;
}) {
  const [mounted, setMounted] = useState(false);
  useEffect(() => setMounted(true), []);
  if (!mounted) return null;
  const target = container ?? document.body;
  return createPortal(children, target);
}
