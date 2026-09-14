import { AdminMenuEditor } from "../admin-menu-editor.tsx";

/** ویرایش منوی موجود. */
export default async function AdminMenuEditPage({
  params,
}: {
  params: Promise<{ menuId: string }>;
}) {
  const { menuId } = await params;
  return <AdminMenuEditor menuId={menuId} />;
}
