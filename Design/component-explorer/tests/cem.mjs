import fs from "node:fs";const c=JSON.parse(fs.readFileSync(process.argv[3]));
const d=c.modules.flatMap(m=>m.declarations||[]).find(x=>x.tagName==="wa-button");
const h=fs.readFileSync(process.argv[2],"utf8"),D=JSON.parse(h.match(/const DATA = ([^\n]*), FONT/)[1]).files["components/button/button.ts"].g;
const own=d.members.filter(m=>!m.inheritedFrom).map(m=>m.name),mine=[...D.Properties,...D.Methods,...D.States].map(x=>x.n);
console.log("CEM only",own.filter(x=>!mine.includes(x)).join(),"CEM own",own.length,"here only",mine.filter(x=>!own.includes(x)).join());
