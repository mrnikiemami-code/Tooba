# Brand distribution — TB-P07-T034

- Most products receive a realistic domain Brand (`demo-brand-*`).
- Brandless rate: deterministic ~15% overall; books/food lean higher (~55% brandless).
- BrandId is nullable; **no fake «بدون برند» Brand entity**.
- Domain brand pools (examples):
  - mobile → samsung/apple/xiaomi/huawei/sony
  - laptop → asus/lenovo/hp/dell/apple
  - clothing → nike/adidas/zara
  - home → bosch/philips/lg/kitchenaid/panasonic
  - food → nestle

Home «برندهای محبوب» continues to show Brand entities only; brandless products do not create empty Brand cards.
