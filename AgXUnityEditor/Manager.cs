using System.Collections.Generic;
using System.Linq;
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

      m_visualsParent = GameObject.Find( m_visualParentName );
      ClearVisualsParent();
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

    public static void OnVisualPrimitiveNodeCreate( Utils.VisualPrimitive primitive )
    {
      if ( primitive == null || primitive.Node == null )
        return;

      if ( m_visualsParent == null ) {
        m_visualsParent = new GameObject( m_visualParentName );
        m_visualsParent.hideFlags = HideFlags.HideAndDontSave;
      }

      if ( primitive.Node.transform.parent != m_visualsParent )
        m_visualsParent.AddChild( primitive.Node );

      m_visualPrimitives.Add( primitive );
    }

    private static Dictionary<KeyCode, Utils.GUIHelper.KeyHandler> m_keyHandlers = new Dictionary<KeyCode, Utils.GUIHelper.KeyHandler>();

    private static string m_visualParentName = "Manager".To32BitFnv1aHash().ToString();
    private static GameObject m_visualsParent = null;
    private static HashSet<Utils.VisualPrimitive> m_visualPrimitives = new HashSet<Utils.VisualPrimitive>();

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
      bool mouseLeftClickNoModifiers = !current.control && !current.shift && !current.alt && current.type == EventType.MouseDown && current.button == 0;
      foreach ( var keyHandler in m_keyHandlers.Values )
        keyHandler.Update( current );

      // Can't perform picking during repaint event.
      if ( current.isMouse ) {
        foreach ( var primitive in m_visualPrimitives ) {
          if ( primitive.Node == null )
            continue;

          HideFlags hideFlags      = primitive.Node.hideFlags;
          primitive.Node.hideFlags = HideFlags.None;
          primitive.MouseOver      = HandleUtility.PickGameObject( current.mousePosition, true ) == primitive.Node;
          primitive.Node.hideFlags = hideFlags;
        }

        if ( mouseLeftClickNoModifiers ) {
          foreach ( var primitive in m_visualPrimitives ) {
            if ( primitive.MouseOver )
              primitive.FireOnMouseClick();
          }
        }
      }

      var proxyTarget = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<OnSelectionProxy>() : null;
      if ( proxyTarget != null )
        Selection.activeGameObject = proxyTarget.Target;

      // TODO: Remove this debug code.
      {
        var edgeDectionTool = GetTool<Tools.EdgeDetectionTool>();
        edgeDectionTool.Target = Application.isPlaying ? null : Selection.activeGameObject;
      }

      foreach ( var tool in m_tools )
        tool.OnSceneViewGUI( sceneView );

      SceneView.RepaintAll();
    }

    private static void ClearVisualsParent()
    {
      if ( m_visualsParent == null )
        return;

      while ( m_visualsParent.transform.childCount > 0 )
        GameObject.DestroyImmediate( m_visualsParent.transform.GetChild( 0 ).gameObject );
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
