"""Blender 5.2 background authoring; no interactive document is touched.
Reuses the original manufactured profiles/material maps, reauthors lengths and joints.
The C# contract is the single dimension source. Original sources remain recoverable.
"""
import bpy, bmesh, math, json, re
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Deliverables/DemeoRework/Source'
ART=ROOT/'Assets/SwitchyardRaiders/RecessedTable/Art'
OUT.mkdir(exist_ok=True); ART.mkdir(parents=True,exist_ok=True)
contract=(ROOT/'Assets/SwitchyardRaiders/Startup/Runtime/TabletopViewContract.cs').read_text()
C={k:float(v) for k,v in re.findall(r'const float (\w+) = ([.\d]+)f;',contract)}
outer=C['OuterSize']; land=C['UsableLandSize']; slab=C['SlabSize']; mount_y=C['TerrainMountHeight']
# Load proven mesh/profile helpers. Only the pre-construction portion executes.
original=(OUT/'legacy_table_profiles.py').read_text()
prefix=original[:original.index('cup_xy=')]
prefix=prefix.replace("ROOT=Path(__file__).resolve().parents[2]", "ROOT=Path("+repr(str(ROOT))+")")
prefix=prefix.replace("OUT=ROOT/'Deliverables/PlayTable'", "OUT=ROOT/'Deliverables/DemeoRework/Source'")
prefix=prefix.replace("ART=ROOT/'Assets/SwitchyardRaiders/PlayTable/Art'", "ART=ROOT/'Assets/SwitchyardRaiders/RecessedTable/Art'")
prefix=prefix.replace("TEX=ART/'Textures'", "TEX=ROOT/'Assets/SwitchyardRaiders/PlayTable/Art/Textures'")
exec(compile(prefix,'original_profile_helpers','exec'))
mount.location.z=mount_y; footprint.location.z=mount_y
footprint['usable_width_m']=land;footprint['usable_depth_m']=land
root['outside_width_m']=outer;root['outside_depth_m']=outer
root['revision']='Task1 structural recessed well, square joinery; no mesh stretching'
# Eight original rolled cup profiles; reduce rail length, retain hole/profile radii.
cup_xy=[(x,y) for y in [-.5875,.5875] for x in [-.30,.30]]+[(x,y) for x in [-.5875,.5875] for y in [-.30,.30]]
buildsrc=original[original.index('def build('):original.index('lod0=build(')]
replacements={
 'side*.6275':'side*.5875','(1.9,.145,.05)':'(outer,.125,.05)',
 'side*.865':'side*.5875','(.17,1.108,.05)':'(.125,1.048,.05)',
 "(0,0,.994),(1.584,1.134,.036)":"(0,0,.865),(1.10,1.10,.030)",
 "(0,0,1.014),(1.56,1.11,.008),'Felt',top,.003":"(0,0,.8825),(1.064,1.064,.005),'Felt',top,.001",
 'side*.55,.946':'side*.55,.9425','(1.69,.03,.106)':'(1.16,.036,.185)',
 'side*.79,0,.946':'side*.55,0,.9425','(.03,1.15,.106)':'(.036,1.064,.185)',
 'x=sx*.83; y=sy*.58':'x=sx*.55; y=sy*.55',
 '(.999,.135,0,0)':'(.85,.135,0,0)',
 'y,.955':'y,.82',
 }
for a,b in replacements.items():
 assert a in buildsrc,a
 buildsrc=buildsrc.replace(a,b)
# Rebate the underside of the rails so the slab never intersects the lip.
rebate="""
 for rail in rails:
  bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,.955))
  cutter=bpy.context.object;cutter.dimensions=(1.064,1.064,.164)
  bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
  mod=rail.modifiers.new('True_rebate_under_inner_lip','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
  bpy.context.view_layer.objects.active=rail
  bpy.ops.object.modifier_move_to_index(modifier=mod.name,index=0);bpy.ops.object.modifier_apply(modifier=mod.name)
  bpy.data.objects.remove(cutter,do_unlink=True)
 """
buildsrc=buildsrc.replace(' # Genuine openings',rebate+'\n # Genuine openings')
# The new continuous well replaces the four old shallow apron pieces.
buildsrc=re.sub(r" for side in \[-1,1\]:\n  box\('Apron_Long_'.*?\n for ix",' for ix',buildsrc,flags=re.S)
# Lip overhang masks the 4mm land edge; lower wall has 3mm clearance to slab.
insert="""
 # Full closed, load-bearing well wall profile, with returned inner lip.
 profile=[(.532,.850),(.568,.850),(.568,1.000),(.532,1.000)]
 vv=[]
 for r,z in profile:
  vv.extend([(-r,-r,z),(r,-r,z),(r,r,z),(-r,r,z)])
 ff=[]
 for j in range(len(profile)):
  for i in range(4):
   ff.append((j*4+i,j*4+(i+1)%4,((j+1)%len(profile))*4+(i+1)%4,((j+1)%len(profile))*4+i))
 well=mesh('Apron_RebatedWell_L'+str(lod),vv,ff,'Oak',top)
 # Cup bodies penetrate the apron. Drill its upper section too, so no wood crosses their bowls.
 for x,y in cup_xy:
  bpy.ops.mesh.primitive_cylinder_add(vertices=segs,radius=.0505,depth=.15,location=(x,y,1.02))
  cutter=bpy.context.object;mod=well.modifiers.new('Cup_socket_clearance','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
  bpy.context.view_layer.objects.active=well;bpy.ops.object.modifier_apply(modifier=mod.name)
  bpy.data.objects.remove(cutter,do_unlink=True)
 uv_box(well);bevel(well,.0007,bevelsegs)
 """
buildsrc=buildsrc.replace(' return group',insert+'\n return group')
exec(compile(buildsrc,'square_joinery','exec'))
lod0=build(0,40,2);lod1=build(1,20,1)
parent_world(mount,pivot);parent_world(footprint,mount)
for c in [author,low]:
 for o in c.objects:
  if o.name.startswith('TabletopAssembly'):parent_world(o,pivot)
exportsrc=original[original.index('def export('):original.index("stats={'LOD0'")]
exportsrc=exportsrc.replace('bmesh.ops.triangulate(bm,faces=list(bm.faces)); bm.to_mesh(me)',"bmesh.ops.triangulate(bm,faces=list(bm.faces)); bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_area()<1e-10],context='FACES'); bm.to_mesh(me)")
exec(compile(exportsrc,'fbx_export','exec'))
stats={'table_LOD0':export(lod0,ART/'SR_RecessedTable_LOD0.fbx'),'table_LOD1':export(lod1,ART/'SR_RecessedTable_LOD1.fbx')}
low.hide_render=True;low.hide_viewport=True
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SR_RecessedTable.blend'))

# Reauthor the land as a closed, regular quad grid cartridge, not a stretched old shell.
# Hidden side detailing is intentionally omitted. Future scenery is unconstrained above datum.
bpy.ops.object.select_all(action='DESELECT')
for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
scene=bpy.context.scene
land_col=bpy.data.collections.new('Editable_land_grid');scene.collection.children.link(land_col)
earth=bpy.data.materials.new('EarthGreen');earth.diffuse_color=(.28,.34,.245,1)
for level,n in [(0,24),(1,8)]:
 vs=[(-slab/2+slab*x/n,-slab/2+slab*y/n,C['SlabHeight']) for y in range(n+1) for x in range(n+1)]
 fs=[]
 for y in range(n):
  for x in range(n):
   a=y*(n+1)+x;fs.append((a,a+1,a+n+2,a+n+1))
 boundary=list(range(n+1))+[y*(n+1)+n for y in range(1,n+1)]+[n*(n+1)+x for x in range(n-1,-1,-1)]+[y*(n+1) for y in range(n-1,0,-1)]
 bottom=[]
 for i in boundary:bottom.append(len(vs));vs.append((vs[i][0],vs[i][1],0))
 for j,i in enumerate(boundary):k=(j+1)%len(boundary);fs.append((i,bottom[j],bottom[k],boundary[k]))
 fs.append(tuple(reversed(bottom)))
 me=bpy.data.meshes.new('LandGrid_L'+str(level));me.from_pydata(vs,[],fs);me.update()
 obj=bpy.data.objects.new(me.name,me);land_col.objects.link(obj);me.materials.append(earth);uv_box(obj)
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
 bpy.ops.export_scene.fbx(filepath=str(ART/('SR_RecessedLand_LOD'+str(level)+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,use_triangles=True)
 stats['land_LOD'+str(level)]={'quads':n*n,'vertices':len(vs)}
 if level==1:obj.hide_render=True;obj.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SR_RecessedLand.blend'))
(OUT/'source-metrics.json').write_text(json.dumps(stats,indent=2))
print('RECESSED_ASSETS_COMPLETE',json.dumps(stats))
