using System.IO;
using UnityEditor;
using UnityEngine;

namespace XueGao.Editor
{
    [InitializeOnLoad]
    internal static class MCPForUnityUvxPathOverride
    {
        private const string UvxPathPreferenceKey = "MCPForUnity.UvxPath";

        static MCPForUnityUvxPathOverride()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string wrapperPath = Path.Combine(projectRoot, "Tools", "mcp-uvx");

            if (!File.Exists(wrapperPath))
            {
                return;
            }

            if (EditorPrefs.GetString(UvxPathPreferenceKey, string.Empty) != wrapperPath)
            {
                EditorPrefs.SetString(UvxPathPreferenceKey, wrapperPath);
            }
        }
    }
}
