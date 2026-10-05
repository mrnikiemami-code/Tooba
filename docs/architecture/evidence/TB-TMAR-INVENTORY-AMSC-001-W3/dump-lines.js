const fs = require('fs');
const lines = fs.readFileSync('docs/architecture/tmar-module-structure-manifests.json', 'utf8').replace(/^\uFEFF/, '').split('\r\n');
for (let n = 1605; n <= 1635; n += 1) {
  console.log(n + ': ' + lines[n - 1]);
}
console.log('total', lines.length);
