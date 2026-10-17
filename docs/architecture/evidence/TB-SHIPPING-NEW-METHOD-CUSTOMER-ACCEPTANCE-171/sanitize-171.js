// TB-171 — sanitize the captured HTTP traces: no session token, guest secret or webhook secret survives.
const fs = require("fs");
const path = require("path");
const dir = __dirname;
for (const file of ["http-trace-on-171.txt", "http-trace-off-171.txt"]) {
    const full = path.join(dir, file);
    if (!fs.existsSync(full)) continue;
    let text = fs.readFileSync(full, "utf8");
    text = text.replace(/"(accessToken|refreshToken|guestSecret|secret|tokenValue)"\s*:\s*"[^"]*"/g, '"$1":"<redacted>"');
    fs.writeFileSync(full, text);
    const leaked = /"(accessToken|refreshToken|guestSecret)":"(?!<redacted>)/.test(text);
    console.log(`${file} leaked=${leaked}`);
}
