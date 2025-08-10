#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace WildSurvival.EditorTools {
  public static class WSGitMenuAdapter {
    [MenuItem(""Wild Survival/Git & Share/Git & Share Hub"", false, 20)]
    public static void Open() {
      // Try the new unified window name first, then known legacy classes
      var typesToTry = new[] {
        ""WildSurvival.EditorTools.WSGitShareHubWindow, Assembly-CSharp-Editor"",
        ""WildSurvival.Editor.Git.GitHubHubWindow, Assembly-CSharp-Editor"",
        ""WildSurvival.Editor.Git.PublicMirrorExporterV2, Assembly-CSharp-Editor"",
        ""WildSurvival.Editor.Share.WildSurvivalShareHub, Assembly-CSharp-Editor""
      };
      foreach (var qn in typesToTry) {
        var t = System.Type.GetType(qn);
        if (t != null && t.IsSubclassOf(typeof(EditorWindow))) {
          EditorWindow.GetWindow(t, false, ""Git & Share Hub"", true).Show();
          return;
        }
      }
      EditorUtility.DisplayDialog(""Git & Share"", ""Could not locate a Git/Share window in imported tools.\nCheck imported scripts and asmdefs."", ""OK"");
    }
  }
}
#endif