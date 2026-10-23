using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SwitchyardRaiders.Startup;
using Object=UnityEngine.Object;
namespace SwitchyardRaiders.Editor
{
    public static class TabletopIntegrationPreview
    {
        [MenuItem("Switchyard Raiders/Tabletop/Render assembled Edit Mode previews")]
        public static void Render()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Edit Mode required");
            var root=PrefabUtility.LoadPrefabContents(TabletopIntegrationSetup.Prefab);
            try
            {
                root.SetActive(true);root.transform.SetPositionAndRotation(new Vector3(0,TabletopViewContract.TerrainMountHeight,0),Quaternion.identity);
                var bindings=root.GetComponent<RaidersTabletopBindings>();bindings.virtualTable.SetActive(true);bindings.terrainAssembly.SetActive(true);bindings.gameplay.gameObject.SetActive(true);bindings.manipulationHandles.SetActive(false);
                foreach(var lod in root.GetComponentsInChildren<LODGroup>(true))
                {
                    var levels=lod.GetLODs();lod.enabled=false;
                    foreach(var level in levels)foreach(var renderer in level.renderers)if(renderer)renderer.enabled=false;
                    if(levels.Length>0)foreach(var renderer in levels[Mathf.Min(1,levels.Length-1)].renderers)if(renderer)renderer.enabled=true;
                }
                for(int i=0;i<bindings.seats.Length;i++)bindings.seats[i].gameObject.SetActive(false);
                for(int i=0;i<4;i++)
                {
                    var train=bindings.playerLocomotives[i];train.gameObject.SetActive(true);float d=i*8+1;
                    TabletopRailway.Place(train,d,true,.0415f);
                }
                var company=bindings.gameplay.Find("Company authored locomotive");TabletopRailway.Place(company,8,true,.0185f);
                for(int i=0;i<6;i++)
                {
                    var wagon=bindings.gameplay.Find("Cargo wagon "+(i+1));wagon.gameObject.SetActive(i<3);float d=8-(i+1)*RaidersMatch.Spacing;
                    TabletopRailway.Place(wagon,d,false,.016f);
                }
                var camGo=new GameObject("Isolated review camera");SceneManager.MoveGameObjectToScene(camGo,root.scene);var camera=camGo.AddComponent<Camera>();camera.scene=root.scene;camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.17f,.16f);camera.nearClipPlane=.01f;camera.farClipPlane=30;camera.fieldOfView=48;
                var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
                void Light(string name,Vector3 rotation,float intensity)
                {var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,root.scene);go.transform.rotation=Quaternion.Euler(rotation);var l=go.AddComponent<Light>();l.type=LightType.Directional;l.intensity=intensity;l.shadows=LightShadows.None;}
                Light("Key",new Vector3(45,-25,0),2);Light("Fill",new Vector3(35,130,0),.9f);
                Directory.CreateDirectory("Deliverables/TabletopIntegration/Previews");
                void Shot(string name,Vector3 position,Vector3 target)
                {
                    camera.transform.position=position;camera.transform.LookAt(target);var rt=new RenderTexture(1600,1100,24);rt.Create();var prior=RenderTexture.active;var image=new Texture2D(1600,1100,TextureFormat.RGB24,false);
                    try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,1100),0,0);image.Apply();File.WriteAllBytes("Deliverables/TabletopIntegration/Previews/"+name+".png",image.EncodeToPNG());}
                    finally{camera.targetTexture=null;RenderTexture.active=prior;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);}
                }
                Shot("assembled-gameplay",new Vector3(2.35f,2.75f,-3.15f),new Vector3(0,.85f,0));
                Shot("playfield-fit",new Vector3(.15f,3.1f,-1.4f),new Vector3(0,1.05f,0));
                bindings.terrainAssembly.SetActive(false);bindings.gameplay.gameObject.SetActive(false);
                Shot("menu-table",new Vector3(2.35f,2.75f,-3.15f),new Vector3(0,.85f,0));
                Debug.Log("Saved three isolated URP camera renders. No PreviewRenderUtility or Play Mode.");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
