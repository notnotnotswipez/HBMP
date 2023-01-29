using System;
using System.IO;

namespace HBMP.Utils
{
    public class DataDirectory
    {
        // Thanks Entanglement
        public static string persistentPath { get; private set; }

        public static void Initialize() {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            persistentPath = appdata + "/HBMPSteamMod/";
            ValidateDirectory(persistentPath);
        }

        public static void ValidateDirectory(string path) {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static string GetPath(string appended) => persistentPath + appended;
    }
}