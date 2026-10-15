// TB-SHIPPING-ONE-LEVEL-TABS-101 — read-only fail-closed probes against the shared Development Host.
const BASE = 'http://127.0.0.1:5088';
const GUEST_CART = '01a13ef6-4fb0-7000-8493-2054009122a1'; // existing Active guest cart (read-only id, no secret known)

async function probe(name, headers, body) {
  const started = new Date().toISOString();
  try {
    const res = await fetch(`${BASE}/v1/storefront/shipping/projection`, {
      method: 'POST',
      headers: { 'content-type': 'application/json', ...headers },
      body: JSON.stringify(body),
    });
    const text = await res.text();
    console.log(`${name} | ${started} | status=${res.status} | body=${text.slice(0, 220)}`);
  } catch (error) {
    console.log(`${name} | ${started} | ERROR ${error.message}`);
  }
}

(async () => {
  console.log('=== TB-101 read-only storefront projection probes (no writes) ===');
  await probe('anonymous-no-token', {}, { cartId: GUEST_CART, language: 'fa' });
  await probe('bogus-bearer', { authorization: 'Bearer not-a-real-session' }, { cartId: GUEST_CART, language: 'fa' });
  await probe('bogus-guest-secret', { 'X-Tooba-Guest-Secret': 'not-the-real-secret' }, { cartId: GUEST_CART, language: 'fa' });
  await probe('unknown-cart-anonymous', {}, { cartId: '00000000-0000-4000-8000-000000000001', language: 'fa' });
  console.log('=== end ===');
})();
