using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using SwitchyardRaiders.Startup;
using Object = UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static class MenuInteractionAudioSetup
    {
        private const string Folder = "Assets/SwitchyardRaiders/Startup/Audio/SpiritSlingUI";
        [MenuItem("Switchyard Raiders/Audio/Connect Spirit Sling UI sounds")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Idle Edit Mode required.");
            if (!Application.dataPath.Replace('\\','/').EndsWith("/Switchyard Raiders/Assets")) throw new InvalidOperationException("Wrong project.");
            var room = Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);
            if (!room || room.gameObject.scene.path != LaunchSequenceTools.ScenePath) throw new InvalidOperationException("Open SR_Launch.");
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(LaunchSequenceTools.Root + "/Audio/SR_Startup.mixer");
            var sfx = mixer ? mixer.FindMatchingGroups("").FirstOrDefault(g => g.name == "SFX") : null;
            if (!sfx) throw new InvalidOperationException("Expected SR_Startup / SFX.");
            var bus = room.GetComponent<MenuInteractionAudio>();
            if (!bus) bus = Undo.AddComponent<MenuInteractionAudio>(room.gameObject);
            Undo.RecordObject(bus, "Connect menu audio");
            bus.hoverClip = Clip("sfx_ui_over_menu");
            bus.confirmClip = Clip("sfx_ui_clic_valid_menu");
            bus.backClip = Clip("sfx_ui_clic_back_menu");
            bus.beginClip = Clip("sfx_ui_clic_begin");
            bus.menuTransitionClip = Clip("sfx_transition_menu");
            bus.countdownClip = Clip("sfx_begin_game_decompte");
            bus.hoverSource = Source(room.transform, "UI hover audio", sfx, .45f);
            bus.clickSource = Source(room.transform, "UI click audio", sfx, .65f);
            bus.transitionSource = Source(room.transform, "UI transition audio", sfx, .4f);
            Undo.RecordObject(room, "Connect menu audio"); room.menuAudio = bus;
            foreach (var button in room.canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var audio = button.GetComponent<MenuButtonAudio>();
                if (!audio) audio = Undo.AddComponent<MenuButtonAudio>(button.gameObject);
                Undo.RecordObject(audio, "Connect button sound"); audio.audioBus = bus;
                bool begins = button == room.mainMenu.matchButton || button == room.mainMenu.friendsButton || button == room.mainMenu.practiceButton;
                audio.clickClip = begins ? bus.beginClip : button.name == "Change room" ? bus.backClip : bus.confirmClip;
                EditorUtility.SetDirty(audio);
            }
            foreach (var c in room.GetComponentsInChildren<Component>(true)) if(c) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            EditorUtility.SetDirty(bus); EditorUtility.SetDirty(room);
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
            if (!EditorSceneManager.SaveScene(room.gameObject.scene)) throw new InvalidOperationException("Scene save failed.");
            PrefabUtility.SaveAsPrefabAsset(room.launch.gameObject, LaunchSequenceTools.Root + "/Prefabs/SR_Launch.prefab", out bool saved);
            if (!saved) throw new InvalidOperationException("Prefab save failed.");
            Validate();
        }
        private static AudioClip Clip(string name)
        {
            var path = Folder + "/" + name + ".wav";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .75f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false; importer.loadInBackground = false;
            importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (!clip) throw new InvalidOperationException("Missing clip " + path);
            return clip;
        }
        private static AudioSource Source(Transform parent, string name, AudioMixerGroup group, float volume)
        {
            var child = parent.Find(name);
            if (!child) { var go = new GameObject(name); go.transform.SetParent(parent,false); Undo.RegisterCreatedObjectUndo(go,"Create UI audio"); child=go.transform; }
            var source = child.GetComponent<AudioSource>();
            if (!source) source = Undo.AddComponent<AudioSource>(child.gameObject);
            Undo.RecordObject(source,"Route UI to SFX");
            source.outputAudioMixerGroup=group; source.playOnAwake=false; source.loop=false;
            source.spatialBlend=0; source.spatialize=false; source.dopplerLevel=0;
            source.volume=volume; source.priority=128; source.clip=null;
            return source;
        }
        [MenuItem("Switchyard Raiders/Audio/Validate UI sounds")]
        public static void Validate()
        {
            var room=Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);
            var bus=room.menuAudio;
            if(!bus || !bus.hoverClip || !bus.confirmClip || !bus.backClip || !bus.beginClip || !bus.menuTransitionClip || !bus.countdownClip)throw new InvalidOperationException("Missing clip references.");
            var report=new System.Text.StringBuilder();
            foreach(var source in new[]{bus.hoverSource,bus.clickSource,bus.transitionSource})
            {
                if(!source || !source.outputAudioMixerGroup || source.outputAudioMixerGroup.name!="SFX" || source.outputAudioMixerGroup.audioMixer.name!="SR_Startup" || source.playOnAwake || source.spatialBlend!=0)throw new InvalidOperationException("Incorrect audio routing.");
                if(source.transform.IsChildOf(room.panelRoot.transform))throw new InvalidOperationException("Audio would be cut off by menu hide.");
                report.AppendLine(source.name+" -> SR_Startup / SFX");
            }
            foreach(var b in room.canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var a=b.GetComponent<MenuButtonAudio>();
                if(!a || a.audioBus!=bus || !a.clickClip || b.GetComponents<MenuButtonAudio>().Length!=1)throw new InvalidOperationException("Missing/duplicate button audio.");
                report.AppendLine(b.name+" -> hover: "+bus.hoverClip.name+"; click: "+a.clickClip.name+"; interactable="+b.interactable);
            }
            report.AppendLine("Room selection and main-menu Show() play sfx_transition_menu. Countdown imported, not connected: no game-start countdown exists yet.");
            report.AppendLine("Static references/import/routing verified. No Play Mode, headset testing or player build.");
            Directory.CreateDirectory("Deliverables/UI_Audio");File.WriteAllText("Deliverables/UI_Audio/validation.txt",report.ToString());
            Debug.Log(report.ToString());
        }
    }
}
