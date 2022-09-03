using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace HBMP.Utils
{
    public static class EmbeddedAssetBundle
    {
        // Thanks Entanglement
        public static byte[] LoadFromAssembly(Assembly assembly, string name) {
            string[] manifestResources = assembly.GetManifestResourceNames();

            if (manifestResources.Contains(name)) {
                using (Stream str = assembly.GetManifestResourceStream(name))
                using (MemoryStream memoryStream = new MemoryStream()) {
                    str.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            return null;
        }
    }
}