export const THEME_MODES = ["LightOnly", "DarkOnly", "System", "UserChoice"] as const;
export type StorefrontThemeMode = (typeof THEME_MODES)[number];
export type StorefrontColorScheme = "light" | "dark";

export const USER_COLOR_SCHEME_COOKIE = "tooba-storefront-color-scheme";
export const USER_COLOR_SCHEME_STORAGE = "tooba.storefront.color-scheme";

export function resolveThemeMode(raw: string | null | undefined): StorefrontThemeMode {
  switch (raw?.trim()) {
    case "Dark":
    case "DarkOnly":
      return "DarkOnly";
    case "System":
      return "System";
    case "UserChoice":
      return "UserChoice";
    default:
      return "LightOnly";
  }
}

export function isKnownThemeMode(raw: string | null | undefined): boolean {
  const value = raw?.trim();
  return value === "Light" || value === "Dark" || THEME_MODES.includes(value as StorefrontThemeMode);
}

export function parseUserColorScheme(raw: string | null | undefined): StorefrontColorScheme | null {
  return raw === "dark" || raw === "light" ? raw : null;
}

export function resolveEffectiveColorScheme(
  mode: StorefrontThemeMode,
  userScheme: StorefrontColorScheme | null,
  systemDark: boolean,
): StorefrontColorScheme {
  if (mode === "DarkOnly") return "dark";
  if (mode === "System") return systemDark ? "dark" : "light";
  if (mode === "UserChoice") return userScheme ?? "light";
  return "light";
}

export const THEME_BOOTSTRAP_SCRIPT = `(function(){var r=document.documentElement;var m=r.getAttribute("data-storefront-theme-mode");function set(d){r.classList.toggle("dark",d);r.setAttribute("data-storefront-color-scheme",d?"dark":"light");}if(m==="LightOnly"){set(false);return;}if(m==="DarkOnly"){set(true);return;}if(m==="System"){set(window.matchMedia("(prefers-color-scheme: dark)").matches);return;}if(m==="UserChoice"){var s=null;try{s=localStorage.getItem("${USER_COLOR_SCHEME_STORAGE}");}catch(e){}if(s!=="light"&&s!=="dark"){var k=document.cookie.match(/(?:^|; )${USER_COLOR_SCHEME_COOKIE}=([^;]*)/);s=k?decodeURIComponent(k[1]):null;}if(s==="dark"||s==="light")set(s==="dark");}})();`;
