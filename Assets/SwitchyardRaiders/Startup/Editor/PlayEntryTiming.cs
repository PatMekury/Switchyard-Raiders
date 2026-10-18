using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SwitchyardRaiders.Editor
{
    [InitializeOnLoad]
    public static class PlayEntryTiming
    {
        private const string StartKey = "SR.PlayTiming.Start";
        static PlayEntryTiming() { EditorApplication.playModeStateChanged += StateChanged; }
        private static void StateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SessionState.SetString(StartKey, EditorApplication.timeSinceStartup.ToString("R",CultureInfo.InvariantCulture));
                SessionState.SetString("SR.PlayTiming.Profile", EditorSettings.enterPlayModeOptions.ToString());
            }
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            if (!double.TryParse(SessionState.GetString(StartKey,""),NumberStyles.Float,CultureInfo.InvariantCulture,out double start)) return;
            double seconds = EditorApplication.timeSinceStartup-start;
            Directory.CreateDirectory(RoomMenuSetup.Report);
            string line = $"{DateTime.Now:s} | {SessionState.GetString("SR.PlayTiming.Profile","")} | Enter Play: {seconds:F3} seconds\n";
            File.AppendAllText(RoomMenuSetup.Report+"/play-entry-timings.txt",line);
            Debug.Log(line);
        }
        [MenuItem("Switchyard Raiders/Room Menu/Measure full reload baseline")]
        public static void Baseline()
        {
            RoomMenuPlaybackCheck.RunFullReload();
        }
    }
}
