\pset pager off
SELECT p.status, count(*) FROM catalog.products p GROUP BY 1 ORDER BY 2 DESC;
SELECT (SELECT count(*) FROM offer.offers) AS offers, (SELECT count(*) FROM pricing.prices) AS prices, (SELECT count(*) FROM inventory.stock_positions) AS stock, (SELECT count(*) FROM catalog.categories) AS categories, (SELECT count(*) FROM catalog.attribute_definitions) AS attributes;
