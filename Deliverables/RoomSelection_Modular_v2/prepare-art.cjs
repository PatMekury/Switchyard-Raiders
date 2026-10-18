const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/patmekury/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const project='C:/Users/patmekury/Documents/Unity/Switchyard Raiders';
const out=path.join(project,'Deliverables/RoomSelection_Modular_v2');
const art=path.join(project,'Assets/SwitchyardRaiders/Startup/RoomMenu/Art/Modular');
fs.mkdirSync(out,{recursive:true});fs.mkdirSync(art,{recursive:true});
async function cropAlpha(input){
 const {data,info}=await sharp(input).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let x0=info.width,y0=info.height,x1=0,y1=0;
 for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>16){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
 if(x1<=x0)throw Error('Missing alpha content');
 return sharp(input).extract({left:x0,top:y0,width:x1-x0+1,height:y1-y0+1}).png().toBuffer();
}
const src='C:/Users/patmekury/.codex/generated_images/01a14507-3e1f-7b72-89a4-986ffc7998c0/';
(async()=>{
 const defs=[
 ['Rails',src+'exec-0eb343c7-cd56-4ff7-ac80-d98408256055.png',1536,660,8,8],
 ['EnamelButton',project+'/Deliverables/UI_Concept_v1/SR_Button_Primary_Concept.png',1024,240,8,692],
 ['IconBackplate',src+'exec-9e369448-95a8-4df3-ad29-992389f7e8a7.png',480,232,1560,8]
 ];
 const layers=[],rects=[];
 for(const [name,file,w,h,x,y] of defs){
 fs.copyFileSync(file,path.join(out,name+'-source.png'));
 const buf=await sharp(await cropAlpha(file)).resize(w,h,{fit:'fill'}).png().toBuffer();
 fs.writeFileSync(path.join(art,name+'.png'),buf);layers.push({input:buf,left:x,top:y});
 rects.push({name,x,y,width:w,height:h});
 }
 for(const [name,old,x] of [['YourRoomIcon','your-room-icon',1560],['VirtualRoomIcon','virtual-room-icon',1800]]){
 let svg=fs.readFileSync(project+'/Deliverables/RoomSelection_Modular_v1/'+old+'.svg','utf8'),n=0;
 svg=svg.replace(/<path\b[^>]*\/>/g,m=>++n<=3?'':m);
 const buf=await sharp(Buffer.from(svg)).resize(224,180,{fit:'contain',background:'#00000000'}).png().toBuffer();
 fs.writeFileSync(path.join(art,name+'.png'),buf);layers.push({input:buf,left:x,top:264});rects.push({name,x,y:264,width:224,height:180});
 }
 const lamp='<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64"><defs><radialGradient id="a" cx=".38" cy=".28" r=".7"><stop stop-color="#ffffff"/><stop offset=".3" stop-color="#eeeece"/><stop offset=".75" stop-color="#8c986f"/><stop offset="1" stop-color="#334334"/></radialGradient></defs><circle cx="32" cy="34" r="28" fill="#162d28"/><circle cx="32" cy="30" r="24" fill="#b89559"/><circle cx="32" cy="30" r="20" fill="url(#a)"/></svg>';
 const lampbuf=await sharp(Buffer.from(lamp)).png().toBuffer();fs.writeFileSync(path.join(art,'SignalLens.png'),lampbuf);layers.push({input:lampbuf,left:1560,top:470});rects.push({name:'SignalLens',x:1560,y:470,width:64,height:64});
 await sharp({create:{width:2048,height:1024,channels:4,background:'#00000000'}}).composite(layers).png().toFile(path.join(art,'RoomMenuAtlas.png'));
 fs.writeFileSync(path.join(out,'atlas-regions.json'),JSON.stringify(rects,null,2));
 console.log(JSON.stringify(rects));
})();
