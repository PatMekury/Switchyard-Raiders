const fs = require('fs');
const path = require('path');
const sharp = require('C:/Users/patmekury/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const out = __dirname;
const C = {cream:'#f4e7c7', edge:'#d6bd89', green:'#20574c', light:'#347366', dark:'#113e37', ink:'#253c36', copper:'#c88753'};
const defs = `<defs><linearGradient id="enamel" x2="0.25" y2="1"><stop stop-color="${C.light}"/><stop offset="1" stop-color="${C.green}"/></linearGradient><linearGradient id="paper" x2="0.4" y2="1"><stop stop-color="#fff2d5"/><stop offset="1" stop-color="#e9d8b4"/></linearGradient></defs>`;
const text=(x,y,s,size=30,fill=C.cream,extra='')=>`<text x="${x}" y="${y}" font-family="Bahnschrift, sans-serif" font-size="${size}" fill="${fill}" ${extra}>${s}</text>`;
function plate(d, fill='url(#paper)', depth=C.edge) {
 return `<path d="${d}" transform="translate(0 9)" fill="${depth}"/><path d="${d}" fill="${fill}"/><path d="${d}" fill="none" stroke="#fff6db" stroke-opacity=".28" stroke-width="2"/>`;
}
const titleShape='M24 13 Q10 14 9 29 L5 73 Q5 95 24 97 L471 102 Q488 102 490 84 L493 30 Q492 15 475 14 L325 8 L178 14 Z';
const heading=plate(titleShape)+`<path d="M32 35 V77 M43 35 V77" stroke="${C.copper}" stroke-width="5"/>`+text(267,67,'CHOOSE YOUR ROOM',31,C.ink,'text-anchor="middle" font-weight="700" letter-spacing="1"');
const iconShape='M54 12 Q22 16 14 47 L4 123 Q3 151 30 163 L121 192 Q141 199 164 186 L237 144 Q257 134 250 106 L232 38 Q229 14 199 12 L125 5 Z';
const iconLines=`fill="none" stroke="${C.ink}" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"`;
const table=`<path d="M70 105 L129 87 L187 106 L128 126 Z M78 109 L78 145 M128 126 L128 157 M180 109 L180 142" ${iconLines}/><path d="M101 105 Q127 91 157 105 Q131 120 101 105 Z" fill="none" stroke="${C.copper}" stroke-width="4"/><path d="M120 102 L136 101 L139 108 L121 110 Z" fill="${C.green}"/>`;
const yourIcon=plate(iconShape)+`<path d="M52 92 L52 54 L128 27 L207 54 L207 91 M128 27 L128 66" ${iconLines}/><path d="M70 59 L99 49 L99 75 L70 85 Z M85 54 V80" fill="none" stroke="${C.copper}" stroke-width="4" stroke-linejoin="round"/>`+table;
const virtualIcon=plate(iconShape)+`<path d="M54 94 V67 Q54 26 126 26 Q201 26 201 67 V94 M69 82 V66 Q71 40 126 40 Q184 40 186 66 V83 M125 42 V68" ${iconLines}/><circle cx="126" cy="22" r="17" fill="${C.green}"/><path d="M126 12 V22 L134 27" stroke="${C.cream}" stroke-width="3" fill="none" stroke-linecap="round"/>`+table;
function button(label,sub,w=425) {
 const d=`M28 7 L${w-29} 1 Q${w-9} 1 ${w-6} 22 L${w-2} 82 Q${w-2} 107 ${w-26} 110 L30 118 Q8 118 6 96 L2 31 Q1 10 28 7 Z`;
 return plate(d,'url(#enamel)',C.dark)+`<path d="M29 16 L${w-31} 11" stroke="#73a58a" stroke-width="3" stroke-linecap="round"/><path d="M18 29 V87" stroke="${C.copper}" stroke-width="5" stroke-linecap="round"/>`+text(w/2+4,53,label,35,C.cream,'text-anchor="middle" font-weight="700" letter-spacing="1"')+text(w/2+4,86,sub,19,'#cbdacc','text-anchor="middle"');
}
const yourButton=button('YOUR ROOM','Play where you are');
const virtualButton=button('VIRTUAL ROOM','Play in a station lounge');
// Purpose-drawn railway route: broad bends, two rails, and individually placed sleepers.
const routePath='M267 188 C213 188 199 249 244 274 C291 301 376 262 429 275 C486 289 498 327 551 329 C664 332 695 258 802 270 C949 288 1036 266 1113 310 C1190 354 1239 435 1206 495';
const ties=[[236,204,-52],[227,235,-2],[243,261,45],[281,276,85],[328,274,95],[378,271,90],[427,276,71],[469,301,43],[513,324,74],[566,329,94],[615,322,109],[661,300,118],[705,279,106],[754,270,89],[803,277,82],[853,281,86],[905,281,90],[958,282,87],[1010,286,81],[1061,297,72],[1107,318,57],[1148,351,45],[1180,391,27],[1204,439,7],[1211,481,-15]];
const route=ties.map(([x,y,a])=>`<path d="M${x-15} ${y} H${x+15}" transform="rotate(${a} ${x} ${y})" stroke="#b78f61" stroke-width="6" stroke-linecap="round"/>`).join('')+`<path d="${routePath}" fill="none" stroke="#d4b782" stroke-width="11"/><path d="${routePath}" fill="none" stroke="#39463e" stroke-width="6"/><circle cx="267" cy="188" r="11" fill="${C.copper}"/><circle cx="1206" cy="495" r="11" fill="${C.copper}"/>`;
const switchLink=`<path d="M737 284 C772 330 776 379 745 430 C716 477 730 542 786 562" fill="none" stroke="#d4b782" stroke-width="10"/><path d="M737 284 C772 330 776 379 745 430 C716 477 730 542 786 562" fill="none" stroke="#39463e" stroke-width="5"/><circle cx="786" cy="562" r="9" fill="${C.copper}"/>`;
const signal=`<path d="M23 43 V98 M7 99 H39" stroke="${C.copper}" stroke-width="7" stroke-linecap="round"/><rect x="7" y="0" width="34" height="56" rx="15" fill="${C.dark}"/><circle cx="24" cy="17" r="9" fill="#809786"/><circle cx="24" cy="39" r="10" fill="#c9dc98"/><circle cx="21" cy="36" r="3" fill="#f0f6c9"/>`;
const wrapper=(body,w,h,bg='')=>`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs}${bg?`<rect width="${w}" height="${h}" fill="${bg}"/>`:''}${body}</svg>`;
const placed=(body,x,y,rotation=0)=>`<g transform="translate(${x} ${y}) rotate(${rotation})">${body}</g>`;
const composition=route+switchLink+placed(heading,413,110,-4)+placed(yourIcon,380,348,-7)+placed(virtualIcon,914,364,6)+placed(yourButton,262,594,-3)+placed(virtualButton,843,620,2)+placed(signal,1090,211,7)+text(800,841,'Touch a button or point and pinch',22,'#e5dfcb','text-anchor="middle"');
async function save(name,body,w,h,bg='') {const svg=wrapper(body,w,h,bg);fs.writeFileSync(path.join(out,name+'.svg'),svg);await sharp(Buffer.from(svg)).png().toFile(path.join(out,name+'.png'));}
(async()=>{
await save('room-selection-preview',composition,1600,960,'#303d36');
await save('room-selection-transparent',composition,1600,960);
await save('heading',placed(heading,8,8),512,132);
await save('your-room-icon',placed(yourIcon,8,8),274,220);
await save('virtual-room-icon',placed(virtualIcon,8,8),274,220);
await save('your-room-button',placed(yourButton,8,8),444,145);
await save('virtual-room-button',placed(virtualButton,8,8),444,145);
await save('railway-route',route+switchLink,1600,960);
await save('signal',placed(signal,8,8),64,122);
console.log('Saved assembled PNGs, seven separate transparent components and editable SVG sources.');
})();
