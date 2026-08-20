import fs from 'node:fs';
const css = fs.readFileSync('C:/Users/user/.nuget/packages/mudblazor/9.7.0/staticwebassets/MudBlazor.min.css', 'utf8');
const re = /[^{}]*mud-divider[^{}]*\{[^}]*\}/g;
let m, r = [];
while ((m = re.exec(css)) && r.length < 30) r.push(m[0].trim());
console.log(r.join('\n---\n'));