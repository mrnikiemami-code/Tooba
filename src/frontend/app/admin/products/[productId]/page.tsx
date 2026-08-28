import { ProductWorkspaceScreen } from "../../product-workspace-screen";

/**
 * مسیر Workspace محصول. منوی CRUD به‌ازای هر ماژول نیست.
 * `scope=view` فقط خواندنی است؛ `scope=edit` ورود مستقیم به ویرایش را درخواست می‌کند.
 * هدر Host از UI می‌رود، نه SpiceDB از کامپوننت عمومی.
 */
export default async function AdminProductWorkspacePage({
  params,
  searchParams,
}: {
  params: Promise<{ productId: string }>;
  searchParams: Promise<{ scope?: string }>;
}) {
  const { productId } = await params;
  const { scope } = await searchParams;
  return (
    <ProductWorkspaceScreen
      productId={productId}
      viewScope={scope === "view"}
      initialEdit={scope === "edit"}
    />
  );
}
