"""Switchyard Raiders play table. Run in Blender 5.2 background, factory startup.
Authoring scene and exports are isolated from any interactive Blender document.
"""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Deliverables/PlayTable'
ART=ROOT/'Assets/SwitchyardRaiders/PlayTable/Art'
TEX=ART/'Textures'
for p in [OUT/'Previews',ART]: p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.name='SR_PlayTable_Authoring'
scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.cycles.use_denoising=True
scene.render.resolution_x=1600; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='AgX'

def collection(name):
 c=bpy.data.collections.new(name); scene.collection.children.link(c); return c
refs=collection('00_Placement_References'); author=collection('01_Table_LOD0_Editable')
low=collection('02_Table_LOD1'); proxycol=collection('90_Fit_Proxy_NOT_FOR_PRODUCTION'); studio=collection('99_Studio_NOT_EXPORTED')
activecol=author
def move(o,c=None):
 for co in list(o.users_collection): co.objects.unlink(o)
 (c or activecol).objects.link(o)
 return o
def empty(name,location=(0,0,0),parent=None,col=None):
 o=bpy.data.objects.new(name,None); (col or refs).objects.link(o)
 o.location=location; o.empty_display_size=.08; o.parent=parent
 return o
root=empty('TableRoot')
pivot=empty('TabletopPivot',(0,0,1.025),root)
mount=empty('TownMount',(0,0,1.018),root)
footprint=empty('TownFootprint',(0,0,1.018),root)
footprint['usable_width_m']=1.56; footprint['usable_depth_m']=1.11
footprint['clearance_per_side_m']=.03
root['nominal_height_m']=1.05
root['notes']='Authoring metres, Z up. Unity conversion Y up. Independent support pieces; no runtime behaviour.'

mats={}
for name in ['Oak','Felt','Stainless','DarkMetal','Brass']:
 m=bpy.data.materials.new('SR_Table_'+name); m.use_nodes=True
 nodes=m.node_tree.nodes; links=m.node_tree.links; bs=nodes.get('Principled BSDF')
 imgs={}
 for i,kind in enumerate(['BaseColor','Normal','Mask']):
  im=bpy.data.images.load(str(TEX/(name+'_'+kind+'.png')),check_existing=True)
  if kind!='BaseColor': im.colorspace_settings.name='Non-Color'
  n=nodes.new('ShaderNodeTexImage'); n.image=im; n.location=(-650,250-i*280); imgs[kind]=n
 links.new(imgs['BaseColor'].outputs['Color'],bs.inputs['Base Color'])
 norm=nodes.new('ShaderNodeNormalMap'); norm.location=(-180,-130)
 links.new(imgs['Normal'].outputs['Color'],norm.inputs['Color']); links.new(norm.outputs['Normal'],bs.inputs['Normal'])
 sep=nodes.new('ShaderNodeSeparateColor'); links.new(imgs['Mask'].outputs['Color'],sep.inputs['Color']); links.new(sep.outputs['Red'],bs.inputs['Metallic'])
 inv=nodes.new('ShaderNodeMath'); inv.operation='SUBTRACT'; inv.inputs[0].default_value=1
 links.new(imgs['Mask'].outputs['Alpha'],inv.inputs[1]); links.new(inv.outputs[0],bs.inputs['Roughness'])
 mats[name]=m

def parent_world(o,p):
 bpy.context.view_layer.update(); w=o.matrix_world.copy(); o.parent=p; o.matrix_world=w
def uv_box(o,grain='X'):
 me=o.data; uv=me.uv_layers.get('UVMap') or me.uv_layers.new(name='UVMap')
 for face in me.polygons:
  n=face.normal; axis=max(range(3),key=lambda i:abs(n[i]))
  for li in face.loop_indices:
   co=me.vertices[me.loops[li].vertex_index].co
   if grain=='Z': a,b=(co.z,co.x if axis==1 else co.y)
   elif grain=='Y': a,b=(co.y,co.x if axis==2 else co.z)
   else: a,b=(co.x,co.y if axis==2 else co.z)
   if axis=={'X':0,'Y':1,'Z':2}[grain]: a,b=(co[(axis+1)%3],co[(axis+2)%3])
   uv.data[li].uv=(a/.85+.31,b/.32+.43)

def mesh(name,verts,faces,mat,parent,grain='X'):
 me=bpy.data.meshes.new(name); me.from_pydata(verts,[],faces); me.update()
 bm=bmesh.new(); bm.from_mesh(me); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
 o=bpy.data.objects.new(name,me); activecol.objects.link(o); o.data.materials.append(mats[mat]); uv_box(o,grain)
 if parent: parent_world(o,parent)
 return o
def bevel(o,width=.004,segs=2):
 m=o.modifiers.new('Manufactured_edge_bevel','BEVEL'); m.width=width; m.segments=segs
 m=o.modifiers.new('Weighted_corner_normals','WEIGHTED_NORMAL'); m.keep_sharp=True; m.weight=40
 return o
def box(name,loc,size,mat,parent,be=.003,segs=2,grain='X'):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=move(bpy.context.object); o.name=name
 o.dimensions=size; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 o.data.materials.append(mats[mat]); uv_box(o,grain)
 if parent: parent_world(o,parent)
 if be: bevel(o,be,segs)
 return o
def lathe(name,profile,loc,mat,parent,segments,axis='Z'):
 verts=[]
 for radius,z in profile:
  for i in range(segments):
   a=2*math.pi*i/segments; p=Vector((radius*math.cos(a),radius*math.sin(a),z))
   if axis=='Y': p=Vector((p.x,p.z,-p.y))
   if axis=='X': p=Vector((p.z,p.x,p.y))
   verts.append(tuple(p+Vector(loc)))
 faces=[]
 for j in range(len(profile)-1):
  for i in range(segments):
   a=j*segments+i; b=j*segments+(i+1)%segments; faces.append((a,b,b+segments,a+segments))
 o=mesh(name,verts,faces,mat,parent)
 for f in o.data.polygons:f.use_smooth=True
 uv=o.data.uv_layers.active
 for face in o.data.polygons:
  for li in face.loop_indices:
   vi=o.data.loops[li].vertex_index; j=vi//segments; i=vi%segments
   u=i/segments
   if face.index%segments==segments-1 and i==0:u=1
   uv.data[li].uv=(u,j/(len(profile)-1))
 return o

cup_xy=[(x,y) for y in [-.6275,.6275] for x in [-.64,.64]]+[(x,y) for x in [-.865,.865] for y in [-.36,.36]]
def build(lod,segs,bevelsegs):
 global activecol
 activecol=author if lod==0 else low
 group=empty('LOD'+str(lod),parent=root,col=activecol)
 top=empty('TabletopAssembly_L'+str(lod),parent=group,col=activecol)
 support=empty('Supports_L'+str(lod),parent=group,col=activecol)
 rails=[]
 for side in [-1,1]:
  rails.append(box('Rail_Long_'+str(side)+'_L'+str(lod),(0,side*.6275,1.025),(1.9,.145,.05),'Oak',top,.008,bevelsegs))
  rails.append(box('Rail_End_'+str(side)+'_L'+str(lod),(side*.865,0,1.025),(.17,1.108,.05),'Oak',top,.007,bevelsegs,'Y'))
 # Genuine openings through the wood; apply booleans before the editable bevel.
 for i,(x,y) in enumerate(cup_xy):
  rail=rails[0 if y<-.55 else 2] if abs(y)>.55 else rails[1 if x<0 else 3]
  bpy.ops.mesh.primitive_cylinder_add(vertices=segs,radius=.0505,depth=.15,location=(x,y,1.02))
  cutter=bpy.context.object
  mod=rail.modifiers.new('Cup_opening_%02d'%i,'BOOLEAN'); mod.operation='DIFFERENCE'; mod.solver='EXACT'; mod.object=cutter
  bpy.context.view_layer.objects.active=rail
  bpy.ops.object.modifier_move_to_index(modifier=mod.name,index=0)
  bpy.ops.object.modifier_apply(modifier=mod.name)
  bpy.data.objects.remove(cutter,do_unlink=True); uv_box(rail,'X' if abs(y)>.55 else 'Y')
  # Rolled lip, inner wall and rounded closed bottom: physically open at the top.
  profile=[(.050, .962),(.050,1.047),(.055,1.048),(.057,1.050),(.056,1.052),(.052,1.053),(.048,1.050),(.0465,1.045),(.043, .978),(.039,.968),(.001,.968),(.001,.962),(.050,.962)]
  lathe('CupHolder_%02d_L%d'%(i+1,lod),profile,(x,y,0),'Stainless',top,segs)
 # Felt is a permanent table inset, not a removable board/tray.
 box('InsetFoundation_L'+str(lod),(0,0,.994),(1.584,1.134,.036),'DarkMetal',top,.003,bevelsegs)
 box('FeltInset_L'+str(lod),(0,0,1.014),(1.56,1.11,.008),'Felt',top,.003,bevelsegs)
 for side in [-1,1]:
  box('Apron_Long_'+str(side)+'_L'+str(lod),(0,side*.55,.946),(1.69,.03,.106),'Oak',top,.004,bevelsegs)
  box('Apron_End_'+str(side)+'_L'+str(lod),(side*.79,0,.946),(.03,1.15,.106),'Oak',top,.004,bevelsegs,'Y')
 for ix,sx in enumerate([-1,1]):
  for iy,sy in enumerate([-1,1]):
   name=('FL','BL','FR','BR')[ix*2+iy]; x=sx*.83; y=sy*.58
   leg=empty('Leg_'+name+'_L'+str(lod),parent=support,col=activecol)
   v=[]
   for z,w,dx,dy in [(.285,.086,sx*.022,sy*.018),(.999,.135,0,0)]:
    for a,b in [(-1,-1),(1,-1),(1,1),(-1,1)]:v.append((x+dx+a*w/2,y+dy+b*w/2,z))
   o=mesh('WoodLeg_'+name+'_L'+str(lod),v,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'Oak',leg,'Z'); bevel(o,.004,bevelsegs)
   bx=x+sx*.022; by=y+sy*.018
   extension=box('Extension_'+name+'_L'+str(lod),(bx,by,.211),(.072,.072,.342),'DarkMetal',leg,.003,bevelsegs,'Z')
   extension['overlap_into_wood_m']=.097
   # Hollow square brass collar, made as four joined strips, not a solid block through the leg.
   for a in [-1,1]:
    box('Collar_X'+str(a)+'_'+name+'_L'+str(lod),(bx+a*.047,by,.301),(.007,.101,.036),'Brass',leg,.002,bevelsegs)
    box('Collar_Y'+str(a)+'_'+name+'_L'+str(lod),(bx,by+a*.047,.301),(.087,.007,.036),'Brass',leg,.002,bevelsegs)
   lathe('Collar_Lock_'+name+'_L'+str(lod),[(r,z*sy) for r,z in [(.001,0),(.009,0),(.009,.006),(.012,.008),(.012,.014),(.009,.017),(.001,.017)]],(bx,by+sy*.05,.301),'Brass',leg,16,'Y')
   lathe('Foot_'+name+'_L'+str(lod),[(.001,0),(.038,0),(.042,.006),(.042,.012),(.035,.018),(.014,.021),(.014,.045),(.001,.045)],(bx,by,0),'DarkMetal',leg,24)
   # Flat inside mounting plates plus broad bolt heads, not ornate trim.
   plate=box('Bracket_'+name+'_L'+str(lod),(x-sx*.073,y,.955),(.006,.115,.070),'DarkMetal',leg,.0015,bevelsegs)
   for dz in [-.018,.018]:
    lathe('Bracket_Bolt_'+name+str(dz)+'_L'+str(lod),[(r,-sx*z) for r,z in [(.001,0),(.006,0),(.006,.003),(.004,.005),(.001,.005)]],(x-sx*.076,y,.955+dz),'Brass',leg,12,'X')
 return group

lod0=build(0,40,2)
lod1=build(1,20,1)
parent_world(mount,pivot)
parent_world(footprint,mount)
for c in [author,low]:
 for o in c.objects:
  if o.name.startswith('TabletopAssembly'):parent_world(o,pivot)
low.hide_render=True; low.hide_viewport=True
activecol=proxycol
proxy=box('TerrainFootprint_PROXY_1500x1050_NOT_PRODUCTION',(0,0,1.078),(1.5,1.05,.12),'Felt',None,.003,1)
proxy.display_type='WIRE'; proxy.hide_render=True; proxy.hide_set(True)
proxy['bottom_m']=1.018; proxy['purpose']='Fit only; excluded from production FBX/prefab. No board or tray.'

# Authoring source stays editable. Export evaluated copies with controlled triangulation.
def export(group,path):
 low.hide_viewport=False
 bpy.context.view_layer.update()
 bpy.ops.object.select_all(action='DESELECT')
 copies=[]; mapping={}
 objs=list(refs.objects)+list((author if group==lod0 else low).objects)
 deps=bpy.context.evaluated_depsgraph_get()
 for o in objs:
  if o.type=='MESH':
   me=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps)
   c=bpy.data.objects.new(o.name,me)
   bm=bmesh.new(); bm.from_mesh(me); bmesh.ops.triangulate(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
  else:c=bpy.data.objects.new(o.name,None)
  scene.collection.objects.link(c); c.matrix_world=o.matrix_world.copy(); mapping[o]=c; copies.append(c)
 for o,c in mapping.items():
  if o.parent in mapping:parent_world(c,mapping[o.parent])
  c.select_set(True)
 stats={'triangles':sum(len(c.data.polygons) for c in copies if c.type=='MESH'),'author_vertices':sum(len(c.data.vertices) for c in copies if c.type=='MESH'),'mesh_objects':sum(c.type=='MESH' for c in copies)}
 bpy.context.view_layer.objects.active=mapping[root]
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'EMPTY','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_space_transform=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',path_mode='RELATIVE',embed_textures=False,bake_anim=False,use_custom_props=True)
 for c in copies:bpy.data.objects.remove(c,do_unlink=True)
 low.hide_viewport=True
 return stats
stats={'LOD0':export(lod0,ART/'SR_PlayTable_LOD0.fbx'),'LOD1':export(lod1,ART/'SR_PlayTable_LOD1.fbx')}
(OUT/'blender_metrics.json').write_text(json.dumps(stats,indent=2))

activecol=studio
floor=box('StudioFloor',(0,0,-.035),(200,200,.06),'Felt',None,0)
floor.data.materials.clear(); fm=bpy.data.materials.new('StudioNeutral'); fm.diffuse_color=(.19,.21,.22,1); fm.use_nodes=True; fm.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.19,.21,.22,1); fm.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.8; floor.data.materials.append(fm)
scene.world.use_nodes=True; scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.55,.60,.67,1); scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.35
for name,loc,power,size in [('Key',(-2,-2.5,4),650,3),('Fill',(2,-.5,3),450,2.5),('Rim',(0,3,3.5),900,2)]:
 data=bpy.data.lights.new(name,'AREA'); data.energy=power; data.shape='DISK'; data.size=size
 o=bpy.data.objects.new(name,data); studio.objects.link(o); o.location=loc; o.rotation_euler=(Vector((0,0,.65))-o.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('ReviewCamera'); cam=bpy.data.objects.new('ReviewCamera',camdata); studio.objects.link(cam); scene.camera=cam
camdata.type='ORTHO'; camdata.lens=50
def render(name,loc,target,scale):
 cam.location=loc; cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler(); camdata.ortho_scale=scale
 scene.render.filepath=str(OUT/'Previews'/('Table_'+name+'.png')); bpy.ops.render.render(write_still=True)
for n,loc,target,scale in [('ThreeQuarter',(2.8,-3.5,2.65),(0,0,.57),2.85),('Top',(0,0,4),(0,0,0),2.3),('Front',(0,-4,.62),(0,0,.62),2.35),('Side',(4,0,.62),(0,0,.62),2.05)]:
 floor.hide_render=n in ['Front','Side']
 render(n,loc,target,scale)
floor.hide_render=False
proxy.hide_set(False); proxy.hide_render=False
render('TerrainFit',(2.8,-3.5,2.65),(0,0,.60),2.85)
proxy.hide_set(True); proxy.hide_render=True
cam.location=(2.8,-3.5,2.65); cam.rotation_euler=(Vector((0,0,.57))-cam.location).to_track_quat('-Z','Y').to_euler(); camdata.ortho_scale=2.85
# Pack image maps for portable source, retain separate PNGs for Unity.
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SR_PlayTable.blend'))
print('TABLE_COMPLETE',json.dumps(stats))
