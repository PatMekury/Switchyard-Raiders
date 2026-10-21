#if UNITY_ANDROID
using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace SwitchyardRaiders.Editor
{
    // Core SDK 207 overwrites supportedDevices even with a custom manifest.
    // Normalize its legacy VR Glasses token after OVRGradleGeneration (order 99999).
    // Store identifiers: https://developers.meta.com/horizon/resources/publish-mobile-manifest
    public sealed class RaidersAndroidManifest : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
        public int callbackOrder => 100000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifest = Path.Combine(path, "src/main/AndroidManifest.xml");
            NormalizeSupportedDevices(manifest);
            Debug.Log("Switchyard Raiders build supportedDevices: " + ReadDevices(manifest));
        }

        private static XmlElement DevicesElement(XmlDocument document)
        {
            var namespaces = new XmlNamespaceManager(document.NameTable);
            namespaces.AddNamespace("android", AndroidNamespace);
            return document.SelectSingleNode(
                "/manifest/application/meta-data[@android:name='com.oculus.supportedDevices']",
                namespaces) as XmlElement
                ?? throw new BuildFailedException("Missing com.oculus.supportedDevices in generated Android manifest.");
        }

        private static string ReadDevices(string path)
        {
            var document = new XmlDocument();
            document.Load(path);
            return DevicesElement(document).GetAttribute("value", AndroidNamespace);
        }

        private static void NormalizeSupportedDevices(string path)
        {
            var document = new XmlDocument { PreserveWhitespace = true };
            document.Load(path);
            var element = DevicesElement(document);
            var original = element.GetAttribute("value", AndroidNamespace);
            var corrected = string.Join("|", original.Split('|')
                .Select(device => device == "stanley" ? "vrglasses" : device).Distinct());
            if (corrected == original) return;
            element.SetAttribute("value", AndroidNamespace, corrected);
            document.Save(path);
        }

        [MenuItem("Switchyard Raiders/Build/Check supported-device manifest regression")]
        public static void CheckRegression()
        {
            if (EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Run this check outside Play Mode and builds.");
            var directory = Path.Combine("Temp", "SRManifestCheck-" + Guid.NewGuid().ToString("N"));
            var main = Path.Combine(directory, "src/main");
            Directory.CreateDirectory(main);
            var manifest = Path.Combine(main, "AndroidManifest.xml");
            try
            {
                File.Copy("Assets/Plugins/Android/AndroidManifest.xml", manifest);
                var metaProcessor = new OVRGradleGeneration();
                var correction = new RaidersAndroidManifest();
                if (correction.callbackOrder <= metaProcessor.callbackOrder)
                    throw new Exception("Manifest correction must run after Meta's Gradle processor.");
                // Exercise the real SDK processor, which caused the rejected new APK.
                metaProcessor.PatchAndroidManifest(directory);
                var generated = ReadDevices(manifest);
                correction.OnPostGenerateGradleAndroidProject(directory);
                var corrected = ReadDevices(manifest);
                var expected = string.Join("|", generated.Split('|')
                    .Select(device => device == "stanley" ? "vrglasses" : device).Distinct());
                if (corrected != expected || corrected.Split('|').Contains("stanley"))
                    throw new Exception("SDK-generated manifest was not corrected.");
                var once = File.ReadAllText(manifest);
                correction.OnPostGenerateGradleAndroidProject(directory);
                if (once != File.ReadAllText(manifest))
                    throw new Exception("Repeated manifest processing changed the result.");
                var report = "PASS: Meta Gradle processing followed by project correction.\n"
                    + "SDK generated: " + generated + "\nCorrected: " + corrected
                    + "\nCallback ordering and repeated processing passed.\nNo APK build or upload performed.\n";
                Directory.CreateDirectory("Deliverables/MainMenuFlow");
                File.WriteAllText("Deliverables/MainMenuFlow/manifest-regression.txt", report);
                Debug.Log(report);
            }
            finally
            {
                // Meta's processor may also create res/xml/network_sec_config.xml.
                // Restrict cleanup to this check's uniquely named project Temp folder.
                var cleanupPath = Path.GetFullPath(directory);
                var tempRoot = Path.GetFullPath("Temp") + Path.DirectorySeparatorChar;
                if (!cleanupPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)
                    || !Path.GetFileName(cleanupPath).StartsWith("SRManifestCheck-", StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected manifest-check cleanup path.");
                Directory.Delete(cleanupPath, true);
            }
        }
    }
}
#endif
