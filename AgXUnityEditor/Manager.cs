using System.Collections.Generic;
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

    private static List<Tools.Tool> m_tools = new List<Tools.Tool>() { new Tools.ShapeResizeTool() };

    private static void OnSceneView( SceneView sceneView )
    {
      var proxyTarget = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<OnSelectionProxy>() : null;
      if ( proxyTarget != null )
        Selection.activeGameObject = proxyTarget.Target;

      foreach ( var tool in m_tools )
        tool.OnSceneViewGUI( sceneView );

      SceneView.RepaintAll();
    }
  }
}
