const codes = [
  'SWRlbXBv', '2KjYp9iy', '2qnYryDa', '2KfYsdiy', '2K3Ys9in', '2qnYp9ix',
  '2K_ZhNuM', '2YXZiNis', '2YfZiNuM', '2YXYqNmE', '2qnZhNuM',
];
for (const c of codes) {
  const s = Buffer.from(c, 'base64').toString('utf8');
  console.log(c, '=>', s, '| cp:', [...s].map((ch) => 'U+' + ch.codePointAt(0).toString(16)).join(' '));
}
