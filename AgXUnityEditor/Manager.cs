using System;
using System.Reflection;
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
    /// The game object mouse is currently over in scene view. Hidden objects,
    /// e.g., VisualPrimitive isn't included in this.
    /// </summary>
    public static GameObject MouseOverObject { get; private set; }

    /// <summary>
    /// True if the current event is left mouse down.
    /// </summary>
    public static bool LeftMouseClick { get; private set; }

    /// <summary>
    /// True if the current event is right mouse down.
    /// </summary>
    public static bool RightMouseClick { get; private set; }

    /// <summary>
    /// True if the right mouse button is pressed (and hold).
    /// </summary>
    public static bool RightMouseDown { get; private set; }

    /// <summary>
    /// True if keyboard escape key is down.
    /// </summary>
    public static bool KeyEscapeDown { get; private set; }

    /// <summary>
    /// True if mouse + key combo is assumed to be a camera control move.
    /// </summary>
    public static bool IsCameraControl { get; private set; }
    
    /// <summary>
    /// Editor data object containing tools/editor specific, session persistent, data.
    /// </summary>
    public static EditorData EditorData { get { return VisualsParent.GetOrCreateComponent<EditorData>(); } }

    /// <summary>
    /// Constructor called when the Unity editor is initialized.
    /// </summary>
    static Manager()
    {
      if ( !System.IO.Directory.Exists( Utils.CustomEditorGenerator.Path ) )
        Debug.LogError( @"Unable to find directory with custom editors: """ + Utils.CustomEditorGenerator.Path + @""". Make sure to import the latest version of PluginStructure.unitypackage." );
      else
        Utils.CustomEditorGenerator.GenerateMissingEditors();

      SceneView.onSceneGUIDelegate             += OnSceneView;
      EditorApplication.hierarchyWindowChanged += OnHierarchyWindowChanged;

      while ( VisualsParent != null && VisualsParent.transform.childCount > 0 )
        GameObject.DestroyImmediate( VisualsParent.transform.GetChild( 0 ).gameObject );

      MouseOverObject = null;
    }

    /// <summary>
    /// Data that tracks certain events when we're hijacking left mouse button.
    /// </summary>
    private class HijackLeftMouseClickData
    {
      public bool AltPressed { get; set; }

      public HijackLeftMouseClickData()
      {
        AltPressed = false;
      }
    };

    /// <summary>
    /// Hijacks left mouse down from the editor and returns true when the button
    /// is released. This is the default behavior of the editor (select @ mouse up)
    /// and it's, without this method, impossible to detect mouse up events.
    /// </summary
    /// <remarks>
    /// Using this method disables the editor default selection behavior.
    /// </remarks>
    /// <returns>True when the hijacked mouse down button is released (i.e., EventType.MouseUp).</returns>
    public static bool HijackLeftMouseClick()
    {
      Event current = Event.current;
      if ( current == null ) {
        Debug.LogError( "Hijack Left Mouse Click can only be used in the GUI event loop." );
        return false;
      }

      EventType currentMouseEventType = current.GetTypeForControl( GUIUtility.GetControlID( FocusType.Passive ) );
      bool hijackMouseDown = currentMouseEventType == EventType.MouseDown &&
                             current.button == 0 &&                           
                            !RightMouseDown &&                                // button 1 is FPS camera movement
                            !current.alt;                                     // alt down is track ball camera movement
      if ( hijackMouseDown ) {
        m_hijackLeftMouseClickData = new HijackLeftMouseClickData();
        GUIUtility.hotControl = 0;
        Event.current.Use();
        return false;
      }

      if ( m_hijackLeftMouseClickData != null ) {
        m_hijackLeftMouseClickData.AltPressed |= current.alt;

        bool leftMouseUp = !m_hijackLeftMouseClickData.AltPressed &&
                            currentMouseEventType == EventType.MouseUp &&
                            Event.current.button == 0;

        if ( currentMouseEventType == EventType.MouseUp )
          m_hijackLeftMouseClickData = null;

        return leftMouseUp;
      }

      return false;
    }

    /// <summary>
    /// Request focus of the scene view window. E.g., when a button is pressed
    /// in the inspector tab and objects in the scene view should respond.
    /// </summary>
    public static void RequestSceneViewFocus()
    {
      m_requestSceneViewFocus = true;
    }

    public static void OnVisualPrimitiveNodeCreate( Utils.VisualPrimitive primitive )
    {
      if ( primitive == null || primitive.Node == null )
        return;

      // TODO: Fix so that "MouseOver" works for newly created primitives.

      if ( primitive.Node.transform.parent != VisualsParent )
        VisualsParent.AddChild( primitive.Node );

      m_visualPrimitives.Add( primitive );
    }

    public static void OnVisualPrimitiveNodeDestruct( Utils.VisualPrimitive primitive )
    {
      if ( primitive == null || primitive.Node == null )
        return;

      primitive.Node.transform.parent = null;
      m_visualPrimitives.Remove( primitive );

      GameObject.DestroyImmediate( primitive.Node );
    }

    private static string m_currentSceneName = string.Empty;
    private static bool m_requestSceneViewFocus = false;
    private static HijackLeftMouseClickData m_hijackLeftMouseClickData = null;

    private static string m_visualParentName = "Manager".To32BitFnv1aHash().ToString();
    private static GameObject m_visualsParent = null;
    private static HashSet<Utils.VisualPrimitive> m_visualPrimitives = new HashSet<Utils.VisualPrimitive>();

    private static GameObject VisualsParent
    {
      get
      {
        if ( m_visualsParent == null ) {
          m_visualsParent = GameObject.Find( m_visualParentName ) ?? new GameObject( m_visualParentName );
          m_visualsParent.hideFlags = HideFlags.HideAndDontSave;
        }

        return m_visualsParent;
      }
    }

    private static void OnSceneView( SceneView sceneView )
    {
      if ( m_requestSceneViewFocus ) {
        sceneView.Focus();
        m_requestSceneViewFocus = false;
      }

      Event current   = Event.current;

      LeftMouseClick  = !current.control && !current.shift && !current.alt && current.type == EventType.MouseDown && current.button == 0;
      KeyEscapeDown   = current.isKey && current.keyCode == KeyCode.Escape && current.type == EventType.KeyUp;

      RightMouseClick = current.type == EventType.MouseDown && current.button == 1;
      if ( RightMouseClick )
        RightMouseDown = true;
      if ( current.type == EventType.MouseUp && current.button == 1 )
        RightMouseDown = false;

      IsCameraControl = current.alt || RightMouseDown;

      foreach ( var primitive in m_visualPrimitives )
        primitive.OnSceneView( sceneView );

      UpdateMouseOverPrimitives( current );

      if ( Selection.activeGameObject != null )
        Selection.activeGameObject = RouteGameObject( Selection.activeGameObject );

      AgXUnity.Rendering.DebugRenderManager.EditorActiveGameObject = Selection.activeGameObject;

      Tools.Tool.HandleOnSceneViewGUI( sceneView );
 
      HandleWindowsGUI( sceneView );

      LeftMouseClick = false;

      SceneView.RepaintAll();
    }

    private static void UpdateMouseOverPrimitives( Event current )
    {
      // Can't perform picking during repaint event.
      if ( current == null || !(current.isMouse || current.isKey) )
        return;

      // Update mouse over before we reveal the VisualPrimitives.
      MouseOverObject = RouteGameObject( HandleUtility.PickGameObject( current.mousePosition, false ) );

      // Early exit if we haven't any active visual primitives.
      if ( m_visualPrimitives.Count == 0 )
        return;

      var primitiveHitList = new[] { new { Primitive = (Utils.VisualPrimitive)null, RaycastResult = Raycast.Hit.Invalid } }.ToList();
      primitiveHitList.Clear();

      Ray mouseRay = HandleUtility.GUIPointToWorldRay( current.mousePosition );
      foreach ( var primitive in m_visualPrimitives ) {
        primitive.MouseOver = false;

        if ( !primitive.Pickable )
          continue;

        Raycast.Hit hit = Raycast.Test( primitive.Node, mouseRay, 500f, true );
        if ( hit.Triangle.Valid )
          primitiveHitList.Add( new { Primitive = primitive, RaycastResult = hit } );
      }

      if ( primitiveHitList.Count == 0 )
        return;

      var bestResult = primitiveHitList[ 0 ];
      for ( int i = 1; i < primitiveHitList.Count; ++i )
        if ( primitiveHitList[ i ].RaycastResult.Triangle.Distance < bestResult.RaycastResult.Triangle.Distance )
          bestResult = primitiveHitList[ i ];

      bestResult.Primitive.MouseOver = true;
      if ( HijackLeftMouseClick() )
        bestResult.Primitive.OnMouseClick( bestResult.RaycastResult, bestResult.Primitive );
    }

    /// <summary>
    /// Routes current game object to the desired object when e.g., selected.
    /// This method uses OnSelectionProxy to find the desired object.
    /// </summary>
    private static GameObject RouteGameObject( GameObject gameObject )
    {
      var proxyTarget = gameObject != null ? gameObject.GetComponent<OnSelectionProxy>() : null;
      return proxyTarget != null ? proxyTarget.Target : gameObject;
    }

    private static void OnHierarchyWindowChanged()
    {
      var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
      if ( scene != null && scene.name != m_currentSceneName ) {
        m_currentSceneName = scene.name;

        if ( !EditorApplication.isPlaying && VisualsParent.GetComponent<EditorData>() != null )
          Component.DestroyImmediate( VisualsParent.GetComponent<EditorData>() );

        // There shouldn't be any MassProperties components since we've
        // changed them to be ScriptAssets.
        AgXUnity.RigidBody[] bodies = UnityEngine.Object.FindObjectsOfType<AgXUnity.RigidBody>();
        foreach ( var rb in bodies ) {
          Component[] components = rb.GetComponents<Component>();
          foreach ( var component in components ) {
            if ( component.GetType() == typeof( AgXUnity.MassProperties ) ) {
              Debug.Log( "Updating RigidBody by removing MassProperties component - copying the values to the new MassProperties instance.", rb.gameObject );
              AgXUnity.MassProperties currentMassProperties = rb.MassProperties;
              FieldInfo[] fields = component.GetType().GetFields( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
              foreach ( var f in fields ) {
                if ( !f.IsNotSerialized )
                  f.SetValue( currentMassProperties, f.GetValue( component ) );
              }
              Component.DestroyImmediate( component );

              UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty( scene );
            }
          }

          // 'm_rb' in MassProperties has been overwritten during the update. Update reference.
          if ( rb.MassProperties.RigidBody == null )
            rb.MassProperties.RigidBody = rb;
        }

        AgXUnity.Constraint[] constraints = UnityEngine.Object.FindObjectsOfType<AgXUnity.Constraint>();
        foreach ( var constraint in constraints ) {
          bool isOldVersion = ( from component in constraint.GetComponents<Component>() where component == null select component ).ToArray().Length > 0 &&
                              constraint.ElementaryConstraints.Length == 0;
          if ( !isOldVersion )
            continue;

          if ( EditorUtility.DisplayDialog( "Update \"" + constraint.name + "\" (type: " + constraint.Type + ") to the new version?",
                                            "The game object will be deleted and a new will be created with the same Reference/Connected setup. All data such as compliance, damping, motor speed etc. will be lost.",
                                            "Update", "Ignore" ) ) {
            AgXUnity.Constraint newConstraint = AgXUnity.Constraint.Create( constraint.Type );

            newConstraint.AttachmentPair.ReferenceObject              = constraint.AttachmentPair.ReferenceObject;
            newConstraint.AttachmentPair.ReferenceFrame.LocalPosition = constraint.AttachmentPair.ReferenceFrame.LocalPosition;
            newConstraint.AttachmentPair.ReferenceFrame.LocalRotation = constraint.AttachmentPair.ReferenceFrame.LocalRotation;

            newConstraint.AttachmentPair.Synchronized                 = constraint.AttachmentPair.Synchronized;

            newConstraint.AttachmentPair.ConnectedObject              = constraint.AttachmentPair.ConnectedObject;
            newConstraint.AttachmentPair.ConnectedFrame.LocalPosition = constraint.AttachmentPair.ConnectedFrame.LocalPosition;
            newConstraint.AttachmentPair.ConnectedFrame.LocalRotation = constraint.AttachmentPair.ConnectedFrame.LocalRotation;

            newConstraint.name = constraint.name;

            GameObject.DestroyImmediate( constraint.gameObject );

            Debug.Log( "Constraint: " + newConstraint.name + " updated.", newConstraint );

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty( scene );
          }
        }
      }
    }

    private static void HandleWindowsGUI( SceneView sceneView )
    {
      SceneViewWindow.OnSceneView( sceneView );
    }
  }
}
