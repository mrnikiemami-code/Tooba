import { AdminLandingPageComposer } from "../admin-landing-page-composer.tsx";

/** ویرایش صفحهٔ فرود. */
export default async function AdminLandingPageEditPage({
  params,
}: {
  params: Promise<{ pageId: string }>;
}) {
  const { pageId } = await params;
  return <AdminLandingPageComposer pageId={pageId} />;
}
