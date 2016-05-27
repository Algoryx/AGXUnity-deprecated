using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class SelectGameObjectDropdownMenuTool : Tool
  {
    public static GUIContent GetGUIContent( GameObject gameObject )
    {
      bool isNull       = gameObject == null;
      bool hasVisual    = !isNull && gameObject.GetComponent<MeshFilter>() != null;
      bool hasRigidBody = !isNull && gameObject.GetComponent<RigidBody>() != null;
      bool hasShape     = !isNull && gameObject.GetComponent<AgXUnity.Collide.Shape>() != null;

      string nullTag      = isNull       ? Utils.GUI.AddColorTag( "[null]", Color.red ) : "";
      string visualTag    = hasVisual    ? Utils.GUI.AddColorTag( "[Visual]", Color.yellow ) : "";
      string rigidBodyTag = hasRigidBody ? Utils.GUI.AddColorTag( "[RigidBody]", Color.Lerp( Color.blue, Color.white, 0.35f ) ) : "";
      string shapeTag     = hasShape     ? Utils.GUI.AddColorTag( "[" + gameObject.GetComponent<AgXUnity.Collide.Shape>().GetType().Name + "]", Color.Lerp( Color.green, Color.white, 0.1f ) ) : "";

      string name = isNull ? "World" : gameObject.name;

      return Utils.GUI.MakeLabel( name + " " + nullTag + rigidBodyTag + shapeTag + visualTag );
    }

    public string Title = "Select game object";

    private GameObject m_target = null;
    public GameObject Target
    {
      get { return m_target; }
      set
      {
        m_target = value;

        BuildListGivenTarget();
      }
    }

    public bool WindowIsActive { get { return SceneViewWindow.GetWindowData( OnWindowGUI ) != null; } }

    public Action<GameObject> OnSelect = delegate { };

    public bool HideOnKeyEscape     = true;
    public bool HideOnCameraControl = true;
    public bool HideOnClickMiss     = true;

    public void Show()
    {
      Show( Event.current.mousePosition );
    }

    public void Show( Vector2 position )
    {
      if ( WindowIsActive )
        return;

      SceneViewWindow.Show( OnWindowGUI, new Vector2( m_windowWidth, 0 ), Event.current.mousePosition + new Vector2( -0.5f * m_windowWidth, -10 ), WindowTitle.text );
    }

    public void Hide()
    {
      m_selected = null;

      SceneViewWindow.Close( OnWindowGUI );
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      bool hide = WindowIsActive && (
                    ( HideOnKeyEscape && Manager.KeyEscapeDown ) ||
                    ( HideOnCameraControl && Manager.IsCameraControl ) ||
                    ( HideOnClickMiss && Manager.LeftMouseClick && !SceneViewWindow.GetWindowData( OnWindowGUI ).Contains( Event.current.mousePosition ) )
                  );

      if ( hide )
        Hide();
      else if ( m_selected != null ) {
        OnSelect( m_selected.Object );
        m_selected = null;
        Hide();
      }
    }

    private GUIContent WindowTitle { get { return Utils.GUI.MakeLabel( Title ); } }

    private List<GameObject> m_gameObjectList = new List<GameObject>();
    private float m_windowWidth = 0f;

    private class SelectedObject { public GameObject Object = null; }
    private SelectedObject m_selected = null;

    private void BuildListGivenTarget()
    {
      m_gameObjectList.Clear();

      m_windowWidth = Mathf.Max( 1.5f * Utils.GUI.Skin.label.CalcSize( WindowTitle ).x, Utils.GUI.Skin.button.CalcSize( GetGUIContent( Target ) ).x );

      if ( Target != null ) {
        m_gameObjectList.Add( Target );

        Transform parent = Target.transform.parent;
        while ( parent != null ) {
          m_windowWidth = Mathf.Max( m_windowWidth, Utils.GUI.Skin.button.CalcSize( GetGUIContent( parent.gameObject ) ).x );

          m_gameObjectList.Add( parent.gameObject );
          parent = parent.parent;
        }
      }

      // Always adding world at end of list. If Target == null this will be the only entry.
      m_gameObjectList.Add( null );
    }

    private void OnWindowGUI( EventType eventType )
    {
      foreach ( GameObject gameObject in m_gameObjectList ) {
        if ( GUILayout.Button( GetGUIContent( gameObject ), Utils.GUI.Skin.button ) )
          m_selected = new SelectedObject() { Object = gameObject };
      }
    }
  }
}
