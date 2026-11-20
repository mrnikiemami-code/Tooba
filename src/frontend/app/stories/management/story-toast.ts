/**
 * Story-owned feedback surface on the app's EXISTING react-toastify container.
 *
 * The shared `ToastContainer` in AppProviders keeps its theme/colors/position and `autoClose=3000`.
 * It also enables `pauseOnHover`/`pauseOnFocusLoss`; those pauses kept Story messages alive while the
 * operator was reading the grid. Story passes per-toast overrides instead of changing the global
 * container: same component/theme/severity colors, but the message always finishes its app duration.
 */
import { toast, type ToastOptions } from "react-toastify";

const STORY_TOAST_OPTIONS: ToastOptions = {
  autoClose: 3000,
  pauseOnHover: false,
  pauseOnFocusLoss: false,
  closeOnClick: true,
};

/** Severity colours come from the existing container theme; no restyling here. */
export const storyToast = {
  success(message: string): void {
    toast.success(message, STORY_TOAST_OPTIONS);
  },
  /** A definite refusal / an operation that cannot run. Never blue "info". */
  error(message: string): void {
    toast.error(message, STORY_TOAST_OPTIONS);
  },
  /** Genuine uncertainty or a partially completed batch. */
  warning(message: string): void {
    toast.warning(message, STORY_TOAST_OPTIONS);
  },
  info(message: string): void {
    toast.info(message, STORY_TOAST_OPTIONS);
  },
};
