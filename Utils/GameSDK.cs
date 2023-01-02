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
            string sdkPath = DataDirectory.GetPath("discord_game_sdk.dll");
            if (!File.Exists(sdkPath))
            {
                File.WriteAllBytes(sdkPath, EmbeddedAssetBundle.LoadFromAssembly(Assembly.GetExecutingAssembly(), "HBMPSteamNetwork.Resources.discord_game_sdk.dll"));
            }
            _ = DllTools.LoadLibrary(sdkPath);
        }
    }
}