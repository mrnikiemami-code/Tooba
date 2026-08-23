"use client";

import { useState } from "react";
import { z } from "zod";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { X } from "lucide-react";
import {
  Accordion,
  Alert,
  AvailabilityBadge,
  Badge,
  Button,
  Card,
  Cluster,
  Dialog,
  DiscountBadge,
  Drawer,
  EmptyState,
  ErrorState,
  Field,
  IconButton,
  Input,
  MediaAspectBox,
  MoneyDisplay,
  PageContainer,
  Popover,
  PricePresentation,
  QuantityControl,
  RatingDisplay,
  SellerIdentityDisplay,
  Skeleton,
  Spinner,
  Stack,
  Switch,
  Tabs,
  ToastRegion,
  Tooltip,
  useTheme,
} from "../../design-system";
import { drawerUsesLogicalStart, iconButtonRequiresLabel } from "../../design-system";
import { GridShowcase } from "./grid-showcase";

const demoSchema = z.object({
  email: z.string().email(),
});

/**
 * ویترین غیرتولیدی. ACCEPT بصری محصول نیست.
 */
export function DesignSystemShowcase() {
  const { theme, setColorScheme, setDirection } = useTheme();
  const [dialog, setDialog] = useState(false);
  const [drawer, setDrawer] = useState(false);
  const [qty, setQty] = useState(1);
  const [toast, setToast] = useState<string | null>(null);
  const form = useForm({ resolver: zodResolver(demoSchema), defaultValues: { email: "" } });

  if (!drawerUsesLogicalStart("start-0") || !iconButtonRequiresLabel("بستن")) {
    throw new Error("design-system invariant failed");
  }

  return (
    <PageContainer>
      <Stack className="py-8">
        <h1 className="ds-display">Tooba Design System</h1>
        <p className="ds-caption text-muted">ویترین داخلی؛ ACCEPT بصری محصول نیست.</p>
        <GridShowcase />
        <Cluster>
          <Button type="button" tone="secondary" onClick={() => setColorScheme(theme.colorScheme === "dark" ? "light" : "dark")}>
            {theme.colorScheme}
          </Button>
          <Button type="button" tone="secondary" onClick={() => setDirection(theme.direction === "rtl" ? "ltr" : "rtl")}>
            {theme.direction}
          </Button>
        </Cluster>
        <Card>
          <Cluster>
            <Button type="button">Primary</Button>
            <Button type="button" tone="secondary">
              Secondary
            </Button>
            <Button type="button" tone="danger">
              Danger
            </Button>
            <IconButton label="بستن">
              <X aria-hidden size={18} />
            </IconButton>
            <Spinner />
            <Skeleton className="w-24" />
          </Cluster>
        </Card>
        <Card>
          <form className="grid gap-3" onSubmit={form.handleSubmit(() => setToast("ذخیره نمایشی"))}>
            <Field id="email" label="ایمیل" hint="جزیره LTR" error={form.formState.errors.email?.message}>
              <Input id="email" ltrIsland invalid={Boolean(form.formState.errors.email)} {...form.register("email")} />
            </Field>
            <Button type="submit">ارسال</Button>
          </form>
        </Card>
        <Alert tone="warning">توکن خطر با برند خام یکی نیست.</Alert>
        <Cluster>
          <Badge tone="success">موفق</Badge>
          <DiscountBadge label="٪ نمایشی" />
          <AvailabilityBadge available />
          <PricePresentation exclusiveAmount="100000" finalAmount="90000" currency="IRR" discounted />
          <MoneyDisplay amount="90000" currency="IRR" />
          <QuantityControl value={qty} onChange={setQty} />
          <RatingDisplay value={4} />
          <SellerIdentityDisplay name="فروشنده نمونه" />
        </Cluster>
        <MediaAspectBox />
        <Tabs
          tabs={[
            { id: "a", label: "یکی", panel: <p>پنل یک</p> },
            { id: "b", label: "دو", panel: <p>پنل دو</p> },
          ]}
        />
        <Accordion items={[{ title: "جزئیات", body: "محتوای جمع‌شونده" }]} />
        <EmptyState title="خالی" detail="دادهٔ دامنه نیست" />
        <ErrorState title="خطا" onRetry={() => setToast("retry")} />
        <Cluster>
          <Button type="button" onClick={() => setDialog(true)}>
            Dialog
          </Button>
          <Button type="button" tone="secondary" onClick={() => setDrawer(true)}>
            Drawer
          </Button>
          <Tooltip label="راهنما">
            <Button type="button" tone="ghost">
              Tooltip
            </Button>
          </Tooltip>
          <Popover trigger={<Button type="button" tone="secondary">Popover</Button>}>
            منوی سبک
          </Popover>
        </Cluster>
        <Switch label="نمونه" />
        <Dialog title="نمونه" open={dialog} onClose={() => setDialog(false)}>
          <p>گفتگوی native.</p>
        </Dialog>
        <Drawer title="کشو" open={drawer} onClose={() => setDrawer(false)}>
          <p>قرارگیری منطقی start.</p>
        </Drawer>
        <ToastRegion message={toast} />
      </Stack>
    </PageContainer>
  );
}
