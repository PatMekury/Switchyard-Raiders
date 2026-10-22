using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace SwitchyardRaiders.CompanyTrain.Editor
{
    public static class CompanyLocomotiveSmokeBuilder
    {
        private const string Root="Assets/SwitchyardRaiders/CompanyLocomotive";
        [MenuItem("Switchyard Raiders/Company Locomotive/Add or rebuild chimney smoke")]
        public static void Build()
        {
            string path=Root+"/Prefabs/CompanyLocomotive.prefab";
            var go=PrefabUtility.LoadPrefabContents(path);
            try{Attach(go);PrefabUtility.SaveAsPrefabAsset(go,path);}
            finally{PrefabUtility.UnloadPrefabContents(go);}
            AssetDatabase.SaveAssets();
        }

        public static void Attach(GameObject locomotive)
        {
            Directory.CreateDirectory(Root+"/VFX");
            string texturePath=Root+"/VFX/ChimneySmokeAtlas.png";
            if(!File.Exists(texturePath))GenerateAtlas(texturePath);
            AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;
            importer.maxTextureSize=512;importer.isReadable=false;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=512,format=TextureImporterFormat.ASTC_6x6});
            importer.SaveAndReimport();
            string materialPath=Root+"/VFX/ChimneySmoke.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_ColorMode",0);
            material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);material.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite",0);material.SetFloat("_Cull",(float)CullMode.Off);
            material.SetFloat("_SoftParticlesEnabled",0);material.SetFloat("_DistortionEnabled",0);material.SetFloat("_FlipbookBlending",0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.DisableKeyword("_SOFTPARTICLES_ON");material.DisableKeyword("_FLIPBOOKBLENDING_ON");
            material.SetOverrideTag("RenderType","Transparent");material.renderQueue=(int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(material);
            Transform outlet=locomotive.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="VFX_ChimneyExhaust");
            Transform existing=outlet.Find("Chimney Smoke");
            var child=existing?existing.gameObject:new GameObject("Chimney Smoke");child.transform.SetParent(outlet,false);
            child.transform.localPosition=Vector3.zero;child.transform.localRotation=Quaternion.identity;child.transform.localScale=Vector3.one;
            var ps=child.GetComponent<ParticleSystem>();if(!ps)ps=child.AddComponent<ParticleSystem>();
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.duration=5;main.loop=true;main.prewarm=false;main.playOnAwake=true;
            main.maxParticles=48;main.startLifetime=new ParticleSystem.MinMaxCurve(1.8f,2.8f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(1.0f,1.65f);main.startSize=new ParticleSystem.MinMaxCurve(.42f,.72f);
            main.startRotation=new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);main.gravityModifier=0;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
            main.cullingMode=ParticleSystemCullingMode.Pause;
            main.startColor=new ParticleSystem.MinMaxGradient(new Color(.52f,.54f,.56f,1),new Color(.78f,.79f,.80f,1));
            var emission=ps.emission;emission.enabled=true;emission.rateOverTime=13;emission.rateOverDistance=0;emission.SetBursts(new ParticleSystem.Burst[0]);
            var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=9;shape.radius=.12f;shape.radiusThickness=1;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.55f,1,2.9f));
            var color=ps.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.84f,.86f,.88f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.60f,.12f),new GradientAlphaKey(.38f,.50f),new GradientAlphaKey(0,1)});color.color=gradient;
            var spin=ps.rotationOverLifetime;spin.enabled=true;spin.z=new ParticleSystem.MinMaxCurve(-.3f,.3f);
            var noise=ps.noise;noise.enabled=true;noise.quality=ParticleSystemNoiseQuality.Low;noise.strength=.16f;noise.frequency=.7f;noise.scrollSpeed=.2f;noise.octaveCount=1;noise.damping=true;
            var sheet=ps.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=2;sheet.numTilesY=2;sheet.animation=ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime=0;sheet.startFrame=new ParticleSystem.MinMaxCurve(0,3.99f);
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;
            renderer.alignment=ParticleSystemRenderSpace.View;renderer.sortMode=ParticleSystemSortMode.Distance;renderer.maxParticleSize=.2f;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            renderer.SetActiveVertexStreams(new System.Collections.Generic.List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
            var control=locomotive.GetComponent<CompanyLocomotiveSmoke>();if(!control)control=locomotive.AddComponent<CompanyLocomotiveSmoke>();
            control.chimneySmoke=ps;control.engineRunning=true;control.engineLoad=.65f;
            EditorUtility.SetDirty(control);
        }

        private static void GenerateAtlas(string path)
        {
            const int tile=256,size=512;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels=new Color32[size*size];
            for(int cell=0;cell<4;cell++)
            for(int y=0;y<tile;y++)
            for(int x=0;x<tile;x++)
            {
                float u=(x+.5f)/tile*2-1,v=(y+.5f)/tile*2-1;
                float seed=cell*17.31f+5.7f,noise=0,weight=.55f,frequency=2.4f;
                for(int octave=0;octave<5;octave++){noise+=weight*Mathf.PerlinNoise(u*frequency+seed,v*frequency+seed*.71f);frequency*=2.03f;weight*=.47f;}
                float warp=Mathf.PerlinNoise(u*3.2f+seed+10,v*3.2f+seed)-.5f;
                float radius=Mathf.Sqrt(u*u+v*v)+warp*.28f;
                float envelope=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.24f,.95f,radius));
                float edge=Mathf.Clamp01((1-Mathf.Max(Mathf.Abs(u),Mathf.Abs(v)))*12);
                float alpha=Mathf.Clamp01(envelope*(.35f+.9f*noise)*edge);
                float light=.68f+.32f*noise;
                pixels[(y+(cell/2)*tile)*size+x+(cell%2)*tile]=new Color(light,light,light,alpha);
            }
            texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
