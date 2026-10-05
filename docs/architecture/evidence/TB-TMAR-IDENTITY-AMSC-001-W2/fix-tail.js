const fs = require('fs');
const p = 'src/backend/Modules/Identity/Tooba.Identity.Endpoints/Auth/IdentityAuthHttpModels.cs';
let t = fs.readFileSync(p, 'utf8');
t = t.replace(/\r\n/g, '\n').replace(/\n/g, '\r\n');
t = t.replace(/\r\n\r\n\}\r\n$/, '\r\n}\r\n');
fs.writeFileSync(p, t, 'utf8');
const crlf = (t.match(/\r\n/g) || []).length;
const lone = (t.match(/(?<!\r)\n/g) || []).length;
console.log('crlf=' + crlf + ' loneLf=' + lone + ' tail=' + JSON.stringify(t.slice(-30)));
