const fs = require('fs');
const body = fs.readFileSync('docs/architecture/tmar-current-state.json', 'utf8').replace(/^\uFEFF/, '');
const lockStart = body.indexOf('"structureLock": {');
const listStart = body.indexOf('"certifiedModules": [', lockStart);
const listEnd = body.indexOf('\n    ],', listStart);
const listText = body.slice(listStart, listEnd);
console.log('lockStart', lockStart, 'listStart', listStart, 'listEnd', listEnd, 'len', listText.length);
console.log(listText);
