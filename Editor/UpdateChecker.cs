using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Wagenheimer.RateControl.Editor
{
    [InitializeOnLoad]
    internal static class UpdateChecker
    {
        const string PackageJsonUrl = "https://raw.githubusercontent.com/wagenheimer/UnityRateControl/master/package.json";
        const string RepoUrl = "https://github.com/wagenheimer/UnityRateControl";
        const string PrefLastCheckTicks = "Wagenheimer.RateControl.UpdateChecker.LastCheckTicks";
        const string PrefSkipVersion = "Wagenheimer.RateControl.UpdateChecker.SkipVersion";
        const double CheckIntervalHours = 24;

        static UpdateChecker()
        {
            EditorApplication.delayCall += () => CheckForUpdate(force: false);
        }

        [MenuItem("Tools/Wagenheimer/Rate Control/Check for Updates")]
        static void CheckForUpdateMenuItem() => CheckForUpdate(force: true);

        static void CheckForUpdate(bool force)
        {
            if (!force && !IntervalElapsed())
                return;

            var request = UnityWebRequest.Get(PackageJsonUrl);
            request.timeout = 5;
            var op = request.SendWebRequest();
            op.completed += _ => OnRequestComplete(request, force);
        }

        static bool IntervalElapsed()
        {
            var stored = EditorPrefs.GetString(PrefLastCheckTicks, "0");
            if (!long.TryParse(stored, out var ticks))
                return true;

            return (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalHours >= CheckIntervalHours;
        }

        static void OnRequestComplete(UnityWebRequest request, bool force)
        {
            EditorPrefs.SetString(PrefLastCheckTicks, DateTime.UtcNow.Ticks.ToString());

            if (request.result != UnityWebRequest.Result.Success)
            {
                request.Dispose();
                return;
            }

            string remoteVersion = null;
            try
            {
                remoteVersion = JsonUtility.FromJson<PackageJsonVersionOnly>(request.downloadHandler.text)?.version;
            }
            catch (Exception)
            {
                // Malformed response — skip silently, will retry on next interval.
            }

            request.Dispose();

            var localVersion = GetLocalVersion();
            if (string.IsNullOrEmpty(remoteVersion) || string.IsNullOrEmpty(localVersion))
                return;

            if (!IsNewer(remoteVersion, localVersion))
                return;

            if (!force && EditorPrefs.GetString(PrefSkipVersion, "") == remoteVersion)
                return;

            UpdateAvailableWindow.Show("Rate Control", localVersion, remoteVersion, RepoUrl, PrefSkipVersion);
        }

        static string GetLocalVersion()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UpdateChecker).Assembly);
            return packageInfo?.version;
        }

        static bool IsNewer(string remote, string local)
        {
            if (Version.TryParse(remote, out var remoteVer) && Version.TryParse(local, out var localVer))
                return remoteVer > localVer;

            return string.CompareOrdinal(remote, local) > 0;
        }

        [Serializable]
        class PackageJsonVersionOnly
        {
            public string version;
        }
    }
}
