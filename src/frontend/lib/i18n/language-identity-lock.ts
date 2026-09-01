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

export function isIdentityFieldLocked(canEdit: boolean | undefined): boolean {
  return canEdit === false;
}
