using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

/// <summary>
/// Applied after every Unity iOS build:
/// 1. ENABLE_USER_SCRIPT_SANDBOXING = NO  — stops il2cpp/rm sandbox denials
/// 2. DEFINES_MODULE = NO (UnityFramework) — stops module-verification cascade errors
/// 3. AppDelegateListener.h include fix    — angle-bracket instead of double-quote
/// 4. NSLocalNetworkUsageDescription       — required for LAN multiplayer (iOS 14+)
/// 5. UIRequiresPersistentWiFi = true      — keeps WiFi alive during gameplay
/// </summary>
public class iOSPostBuild
{
#if UNITY_IOS
    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.iOS) return;

        // ── 1 & 2: pbxproj settings ──────────────────────────────────────────
        string projPath = buildPath + "/Unity-iPhone.xcodeproj/project.pbxproj";
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);

        string mainGuid      = proj.GetUnityMainTargetGuid();
        string frameworkGuid = proj.GetUnityFrameworkTargetGuid();
        string gameAssembly  = proj.TargetGuidByName("GameAssembly");

        // Fix sandbox violations (macOS Sequoia build-phase restriction)
        foreach (var guid in new[] { mainGuid, frameworkGuid, gameAssembly })
        {
            if (string.IsNullOrEmpty(guid)) continue;
            proj.SetBuildProperty(guid, "ENABLE_USER_SCRIPT_SANDBOXING", "NO");
        }

        // Disable Clang module generation for UnityFramework (not a Swift module)
        if (!string.IsNullOrEmpty(frameworkGuid))
            proj.SetBuildProperty(frameworkGuid, "DEFINES_MODULE", "NO");

        proj.WriteToFile(projPath);

        // ── 3: fix double-quoted include in framework public header ──────────
        string adlPath = Path.Combine(buildPath, "Classes/PluginBase/AppDelegateListener.h");
        if (File.Exists(adlPath))
        {
            string txt = File.ReadAllText(adlPath);
            string fixed_txt = txt.Replace(
                "#include \"LifeCycleListener.h\"",
                "#include <UnityFramework/LifeCycleListener.h>");
            if (txt != fixed_txt)
                File.WriteAllText(adlPath, fixed_txt);
        }

        // ── 4 & 5: Info.plist — local network + persistent WiFi ──────────────
        // iOS 14+ silently blocks UDP (port 7777) without NSLocalNetworkUsageDescription.
        // UIRequiresPersistentWiFi prevents iOS from dropping WiFi mid-game.
        string plistPath = buildPath + "/Info.plist";
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        var root = plist.root;

        root.SetString("NSLocalNetworkUsageDescription",
            "Boober Dash uses local Wi-Fi to connect with players on the same network.");
        root.SetBoolean("UIRequiresPersistentWiFi", true);

        plist.WriteToFile(plistPath);

        UnityEngine.Debug.Log("[iOSPostBuild] All fixes applied (sandbox, modules, ADL header, LAN permission, WiFi).");
    }
#endif
}
