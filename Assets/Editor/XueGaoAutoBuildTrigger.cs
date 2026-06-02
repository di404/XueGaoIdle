using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class XueGaoAutoBuildTrigger
{
    private const string RelativeTriggerPath = "Editor/.xuegao_autobuild";

    static XueGaoAutoBuildTrigger()
    {
        RunIfTriggered();
    }

    [InitializeOnLoadMethod]
    private static void RunIfTriggered()
    {
        string triggerPath = Path.Combine(Application.dataPath, RelativeTriggerPath);
        if (!File.Exists(triggerPath))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            string delayedTriggerPath = Path.Combine(Application.dataPath, RelativeTriggerPath);
            if (!File.Exists(delayedTriggerPath))
            {
                return;
            }

            File.Delete(delayedTriggerPath);
            Debug.Log("XueGao auto build trigger detected. Building formal game assets.");
            XueGaoProjectBuilder.BuildFormalGame();
        };
    }
}
