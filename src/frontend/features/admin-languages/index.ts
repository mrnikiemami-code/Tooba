/**
 * Public boundary for admin languages capability.
 * External consumers must import from this module, not deep internals.
 */
export { AdminLanguagesScreen } from "./components/language-list.tsx";
export {
  loadAdminLanguages,
  updateAdminLanguage,
  patchAdminLanguage,
} from "./api/language-api.ts";
