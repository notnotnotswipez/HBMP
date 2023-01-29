using System.IO;
using System.Reflection;
using MelonLoader;

namespace HBMP.Utils
{
    public class GameSDK
    {
        // Thanks Entanglement
        public static void LoadGameSDK()
        {
            foreach (var names in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                MelonLogger.Msg("Resource found: "+names);
            }
            string sdkPath = DataDirectory.GetPath("discord_game_sdk.dll");
            if (!File.Exists(sdkPath))
            {
                File.WriteAllBytes(sdkPath, EmbeddedAssetBundle.LoadFromAssembly(Assembly.GetExecutingAssembly(), "HBMP.Resources.discord_game_sdk.dll"));
            }
            _ = DllTools.LoadLibrary(sdkPath);
        }
    }
}