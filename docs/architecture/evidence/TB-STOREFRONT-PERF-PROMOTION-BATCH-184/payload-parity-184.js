// TB-184 payload parity + item integrity across size A and size B.
const fs = require('fs');
const dir = 'docs/architecture/evidence/TB-STOREFRONT-PERF-PROMOTION-BATCH-184/';
const read = (f) => JSON.parse(fs.readFileSync(dir + f, 'utf8').replace(/^\uFEFF/, ''));
const A = read('home-size-a.json');
const B = read('home-size-b.json');
const secs = ['featuredProducts', 'specialOffers', 'campaignProducts', 'newArrivals', 'productRail', 'mostViewedProducts', 'brands', 'categories', 'bestSellerColumns', 'homeCategories', 'featuredReviews', 'latestArticles'];
const ids = (o) => [...new Set([...o.featuredProducts, ...o.specialOffers, ...o.campaignProducts, ...o.newArrivals, ...o.productRail, ...o.mostViewedProducts].filter((x) => x && x.productId).map((x) => x.productId))];
const cards = (o) => [...o.featuredProducts, ...o.specialOffers, ...o.newArrivals, ...o.productRail, ...o.mostViewedProducts].filter((x) => x && x.productId);
const integrity = (o) => {
    const c = cards(o);
    return {
        cardOccurrences: c.length,
        distinctProducts: new Set(c.map((x) => x.productId)).size,
        missingPrice: c.filter((x) => x.offerAmountExclusiveOfTax == null).length,
        missingSeller: c.filter((x) => !x.sellerDisplayName && !x.sellerName).length,
        missingCurrency: c.filter((x) => !x.currency).length,
        currencies: [...new Set(c.map((x) => x.currency))],
        missingStock: c.filter((x) => x.availableUnits == null).length,
        missingSlug: c.filter((x) => !x.slug).length,
        withPromotionAmount: c.filter((x) => x.promotionalAmountExclusiveOfTax != null).length,
    };
};
const sections = {};
for (const s of secs) sections[s] = [A[s] ? A[s].length : 0, B[s] ? B[s].length : 0];
const out = {
    note: 'Left = size A (3 published), right = size B (12 published). Sections are allowed to differ only through published-population, never through price/seller/stock shape.',
    sections,
    distinctProductIds: { a: ids(A).length, b: ids(B).length },
    sizeAIntegrity: integrity(A),
    sizeBIntegrity: integrity(B),
    productIdSamplesB: ids(B).slice(0, 12),
};
fs.writeFileSync(dir + 'payload-parity-184.json', JSON.stringify(out, null, 2));
console.log(JSON.stringify(out, null, 2));
