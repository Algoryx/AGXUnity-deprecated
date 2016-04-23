using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AgXUnity.Utils;

namespace AgXUnityEditor
{
  /// <summary>
  /// Manager object, initialized when the Unity editor is loaded, to handle
  /// all tools, behavior related etc. objects while in edit mode.
  /// </summary>
  [InitializeOnLoad]
  public static class Manager
  {
    /// <summary>
    /// Constructor called when the Unity editor is initialized.
    /// </summary>
    static Manager()
    {
      SceneView.onSceneGUIDelegate += OnSceneView;
    }

    /// <summary>
    /// Callback from KeyHandler objects when constructed. The KeyCode has to be unique.
    /// </summary>
    public static void OnKeyHandlerConstruct( Utils.GUIHelper.KeyHandler handler )
    {
      if ( m_keyHandlers.ContainsKey( handler.Key ) ) {
        Debug.LogWarning( "Key handler with key: " + handler.Key + " already registered. Ignoring handler." );
        return;
      }

      m_keyHandlers.Add( handler.Key, handler );
    }

    private static Dictionary<KeyCode, Utils.GUIHelper.KeyHandler> m_keyHandlers = new Dictionary<KeyCode, Utils.GUIHelper.KeyHandler>();

    private static List<Tools.Tool> m_tools = new List<Tools.Tool>()
    {
      new Tools.ShapeResizeTool()
      {
        ActivateKey       = new Utils.GUIHelper.KeyHandler( KeyCode.LeftControl ),
        SymmetricScaleKey = new Utils.GUIHelper.KeyHandler( KeyCode.LeftShift )
      },
      new Tools.EdgeDetectionTool()
    };

    private static void OnSceneView( SceneView sceneView )
    {
      Event current = Event.current;
      foreach ( var keyHandler in m_keyHandlers.Values )
        keyHandler.Update( current );

      var proxyTarget = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<OnSelectionProxy>() : null;
      if ( proxyTarget != null )
        Selection.activeGameObject = proxyTarget.Target;

      // TODO: Remove this debug code.
      {
        var edgeDectionTool = GetTool<Tools.EdgeDetectionTool>();
        edgeDectionTool.Target = Selection.activeGameObject;
      }

      foreach ( var tool in m_tools )
        tool.OnSceneViewGUI( sceneView );

      SceneView.RepaintAll();
    }

    private static T GetTool<T>() where T : Tools.Tool
    {
      foreach ( var tool in m_tools )
        if ( tool is T )
          return tool as T;
      return null;
    }
  }
}
