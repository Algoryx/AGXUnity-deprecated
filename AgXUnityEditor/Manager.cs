using UnityEngine;
using UnityEditor;
using AgXUnity.Utils;

namespace AgXUnityEditor
{
  [InitializeOnLoad]
  public static class Manager
  {
    static Manager()
    {
      SceneView.onSceneGUIDelegate += OnSceneView;
    }

    private static void OnSceneView( SceneView sceneView )
    {
      var proxyTarget = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<OnSelectionProxy>() : null;
      if ( proxyTarget != null )
        Selection.activeGameObject = proxyTarget.Target;

      SceneView.RepaintAll();
    }
  }
}
