from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
folder=Path(__file__).parent/"Task5Evidence"
font=ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf",22)
small=ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf",18)
bold=ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf",29)
def compose(name,items,columns,width=800):
    tile_h=550; caption=100; header=105; rows=(len(items)+columns-1)//columns
    canvas=Image.new("RGB",(width*columns,header+(tile_h+caption)*rows+55),(18,24,28));draw=ImageDraw.Draw(canvas)
    draw.text((22,14),"SWITCHYARD RAIDERS | MR SIZE CALIBRATION",font=bold,fill=(239,225,180))
    draw.text((22,57),"SYNTHETIC EDIT MODE - actual assets. Calculated baseline; no passthrough or observed headset MR.",font=small,fill=(202,216,224))
    for i,(file,title,line1,line2) in enumerate(items):
        x=(i%columns)*width;y=header+(i//columns)*(tile_h+caption)
        im=Image.open(folder/file).resize((width,tile_h),Image.Resampling.LANCZOS);canvas.paste(im,(x,y))
        draw.text((x+16,y+tile_h+5),title,font=font,fill=(241,226,181))
        draw.text((x+16,y+tile_h+38),line1,font=small,fill=(220,228,234))
        draw.text((x+16,y+tile_h+64),line2,font=small,fill=(185,202,213))
    draw.text((22,canvas.height-40),"No geometry stretched. LOD0 forced for comparison; rendered detail is not a headset performance result.",font=small,fill=(181,196,205))
    canvas.save(folder/name,quality=94)
compose("mr-before-after.jpg",[
 ("before-top.png","BEFORE | recorded old scale s=1","Land 1.500 x 1.050 m | engine 0.054464 m","Old MR hid the complete virtual table. Same top camera."),
 ("after-top.png","AFTER | normal MR, relative zoom 1","Land 2.510 m square | outer tray 3.108 m square","Engine 0.353788 m | one presentation scale: 2.390457"),
 ("before-seated.png","BEFORE | illustrative seated placement","Old land on a 0.75 m table; centre 1 m forward","Eye 1.30 m; 65 degree vertical FOV. Calculated fixture."),
 ("after-seated-1.png","AFTER | free MR at normal size","Near rim 0.30 m forward; rim 0.50 m below eyes","Same camera and FOV. New centre 1.854 m forward.")],2)
compose("mr-zoom-stages.jpg",[
 ("after-seated-1.png","NORMAL | relative zoom 1","Tray 3.108 m | land 2.510 m | engine 0.354 m","Presentation scale 2.390457 | engine width 17% of frame"),
 ("after-seated-3.png","INTERMEDIATE | relative zoom 3","Tray 9.323 m | land 7.530 m | engine 1.061 m","Presentation scale 7.171371 | engine width 67% of frame"),
 ("after-seated-6.png","IMMERSIVE | relative zoom 6","Tray 18.646 m | land 15.060 m | engine 2.123 m","Presentation scale 14.342742 | mechanical detail fills view")],3)
