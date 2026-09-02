export interface LanguageIdentityLockCopy {
  fa: string;
  en: string;
}

export function codeLockExplanation(): LanguageIdentityLockCopy {
  return {
    fa: "این زبان در محتوا استفاده شده است و کد آن قابل تغییر نیست.",
    en: "This language is used in content and its code cannot be changed.",
  };
}

export function urlPrefixLockExplanation(): LanguageIdentityLockCopy {
  return {
    fa: "این زبان در آدرس‌های محتوا استفاده شده است و پیشوند مسیر قابل تغییر نیست.",
    en: "This language is used in content URLs and its route prefix cannot be changed.",
  };
}

/** یادداشت کوتاه: پیشوندهای جدید فراتر از localeهای build-time ویترین نیاز به rebuild دارند. */
export function storefrontLocalePrefixDeployNote(): LanguageIdentityLockCopy {
  return {
    fa: "پیشوندهای URL جدید فراتر از زبان‌های build-time ویترین (fa/en) نیاز به rebuild/deploy دارند؛ رجیستری به‌تنهایی پیشوند دلخواه را فوری قابل‌مسیریابی نمی‌کند.",
    en: "New URL prefixes beyond build-time storefront locales (fa/en) need a rebuild/deploy; the registry alone does not make arbitrary prefixes routable.",
  };
}

export function isIdentityFieldLocked(canEdit: boolean | undefined): boolean {
  return canEdit === false;
}
