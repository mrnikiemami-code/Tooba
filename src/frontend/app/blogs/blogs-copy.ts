/**
 * کپی پوستهٔ مجله — فقط fa/en بدون زیرسیستم i18n جدید.
 */

export type BlogsShellCopy = {
  title: string;
  loading: string;
  empty: string;
  readMore: string;
  all: string;
  prev: string;
  next: string;
  backToMagazine: string;
  notFound: string;
  featured: string;
  imageBadge: string;
  magazineFooter: string;
  categoryHeading: string;
  authorHeading: string;
  articlesEmpty: string;
};

const FA: BlogsShellCopy = {
  title: "مجله توبا",
  loading: "در حال بارگذاری مقالات…",
  empty: "هنوز مقالهٔ منتشرشده‌ای وجود ندارد.",
  readMore: "مطالعه",
  all: "همه",
  prev: "قبلی",
  next: "بعدی",
  backToMagazine: "بازگشت به مجله",
  notFound: "مقاله پیدا نشد",
  featured: "ویژه",
  imageBadge: "تصویر",
  magazineFooter: "محتوای منتشرشده از ماژول Content",
  categoryHeading: "دسته",
  authorHeading: "نویسنده",
  articlesEmpty: "مقاله‌ای در این فهرست نیست.",
};

const EN: BlogsShellCopy = {
  title: "Tooba Magazine",
  loading: "Loading articles…",
  empty: "No published articles yet.",
  readMore: "Read more",
  all: "All",
  prev: "Previous",
  next: "Next",
  backToMagazine: "Back to magazine",
  notFound: "Article not found",
  featured: "Featured",
  imageBadge: "Image",
  magazineFooter: "Published content from the Content module",
  categoryHeading: "Category",
  authorHeading: "Author",
  articlesEmpty: "No articles in this list.",
};

/** کپی پوسته بر اساس locale ویترین (fa / en). */
export function blogsCopy(locale: string): BlogsShellCopy {
  return locale === "fa" ? FA : EN;
}

/** مسیر داخلی دستهٔ مجله. */
export function blogsCategoryPath(slug: string): string {
  return `/blogs/category/${slug}`;
}

/** مسیر داخلی نویسندهٔ مجله. */
export function blogsAuthorPath(slug: string): string {
  return `/blogs/author/${slug}`;
}
