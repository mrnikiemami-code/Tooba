"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  createProductWorkspaceDirtyRegistry,
  type ProductWorkspaceDirtyRegistry,
} from "./product-workspace-dirty";

const ProductWorkspaceDirtyContext = createContext<ProductWorkspaceDirtyRegistry | null>(null);

/**
 * Provides a workspace-scoped dirty registry so panels can register unsaved edits
 * and the shell can guard tab switch / exit-edit without silent data loss.
 */
export function ProductWorkspaceDirtyProvider({ children }: { children: ReactNode }) {
  const [, setVersion] = useState(0);
  const bump = useCallback(() => setVersion((v) => v + 1), []);
  const registry = useMemo(() => createProductWorkspaceDirtyRegistry(bump), [bump]);

  return (
    <ProductWorkspaceDirtyContext.Provider value={registry}>{children}</ProductWorkspaceDirtyContext.Provider>
  );
}

export function useProductWorkspaceDirtyRegistry(): ProductWorkspaceDirtyRegistry {
  const registry = useContext(ProductWorkspaceDirtyContext);
  if (!registry) {
    throw new Error("useProductWorkspaceDirtyRegistry requires ProductWorkspaceDirtyProvider");
  }
  return registry;
}

/**
 * Registers (and keeps updated) a section's dirty flag + discard callback.
 * Unregisters on unmount so inactive tabs do not leave stale entries.
 * No-ops when rendered outside ProductWorkspaceDirtyProvider (standalone pages).
 */
export function useProductWorkspaceDirtyRegistration(
  sectionId: string,
  isDirty: boolean,
  discard: () => void,
): void {
  const registry = useContext(ProductWorkspaceDirtyContext);

  useEffect(() => {
    if (!registry) return;
    registry.register(sectionId, { isDirty, discard });
    return () => registry.unregister(sectionId);
  }, [registry, sectionId, isDirty, discard]);
}
