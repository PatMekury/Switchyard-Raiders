const fs=require('fs');
const sharp=require('C:/Users/patmekury/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const project='C:/Users/patmekury/Documents/Unity/Switchyard Raiders';
const art=project+'/Assets/SwitchyardRaiders/Startup/RoomMenu/Art/Modular';
const out=project+'/Deliverables/RoomSelection_Modular_v3';
const src='C:/Users/patmekury/.codex/generated_images/01a14507-3e1f-7b72-89a4-986ffc7998c0/';
(async()=>{
 const layers=[];
 for(const [name,file,y] of [['StationHeading','exec-9ef3cda2-08dc-48df-8f97-da2c0b93b498.png',0],['RaisedButtonCap','exec-b7097323-d701-4757-adca-529a4880b1a1.png',304]]){
  fs.copyFileSync(src+file,out+'/'+name+'-source.png');
  const {data,info}=await sharp(src+file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let minX=info.width,minY=info.height,maxX=0,maxY=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>24){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);}
  const png=await sharp(src+file).extract({left:minX,top:minY,width:maxX-minX+1,height:maxY-minY+1}).resize(1008,272,{fit:'fill'}).extend({top:8,bottom:8,left:8,right:8,background:'#00000000'}).png().toBuffer();
  fs.writeFileSync(art+'/'+name+'.png',png);fs.writeFileSync(out+'/'+name+'.png',png);layers.push({input:png,left:0,top:y});
 }
 await sharp({create:{width:1024,height:1024,channels:4,background:'#00000000'}}).composite(layers).png().toFile(art+'/MechanicalMenuAtlas.png');
 console.log('Saved heading/cap transparent PNGs and 1024x1024 atlas.');
})();
