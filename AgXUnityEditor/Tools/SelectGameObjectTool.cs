using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class SelectGameObjectTool : Tool
  {
    private GameObject m_orgSelected = null;
    private GameObject m_selected = null;
    private bool m_objectSelected = false;
    private List<GameObject> m_gameObjectsToChoose = new List<GameObject>();
    private Action<GameObject> m_onSelectedCallback = null;

    private GUIContent WindowTitle { get { return Utils.GUIHelper.MakeLabel( "Select game object" ); } }

    public bool SelectionWindowActive { get { return m_gameObjectsToChoose.Count > 0; } }

    public SelectGameObjectTool( Action<GameObject> onSelectedCallback )
    {
      m_orgSelected = Selection.activeGameObject;
      m_onSelectedCallback = onSelectedCallback;
    }

    public void Clear()
    {
      m_gameObjectsToChoose.Clear();
      SceneViewWindow.Close( OnMultipleOptions );
      m_selected = null;
      m_objectSelected = false;
    }

    public static GUIContent GetGUIContent( GameObject gameObject )
    {
      bool isNull       = gameObject == null;
      bool hasVisual    = !isNull && gameObject.GetComponent<MeshFilter>() != null;
      bool hasRigidBody = !isNull && gameObject.GetComponent<RigidBody>() != null;
      bool hasShape     = !isNull && gameObject.GetComponent<AgXUnity.Collide.Shape>() != null;

      string nullTag      = isNull       ? Utils.GUIHelper.AddColorTag( "[null]", Color.red ) : "";
      string visualTag    = hasVisual    ? Utils.GUIHelper.AddColorTag( "[Visual]", Color.yellow ) : "";
      string rigidBodyTag = hasRigidBody ? Utils.GUIHelper.AddColorTag( "[RigidBody]", Color.Lerp( Color.blue, Color.white, 0.35f ) ) : "";
      string shapeTag     = hasShape     ? Utils.GUIHelper.AddColorTag( "[" + gameObject.GetComponent<AgXUnity.Collide.Shape>().GetType().Name + "]", Color.Lerp( Color.green, Color.white, 0.1f ) ) : "";

      string name = isNull ? "World" : gameObject.name;

      return Utils.GUIHelper.MakeLabel( name + " " + nullTag + rigidBodyTag + shapeTag + visualTag );
    }

    public override void OnRemove()
    {
      SceneViewWindow.Close( OnMultipleOptions );
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      sceneView.Focus();

      bool isMouseClick = Manager.HijackLeftMouseClick();
      bool clickAndMiss = isMouseClick && SelectionWindowActive && !SceneViewWindow.GetWindowData( OnMultipleOptions ).Contains( Event.current.mousePosition );
      if ( clickAndMiss )
        Clear();
      else if ( isMouseClick && !SelectionWindowActive ) {
        float buttonMaxWidth = Mathf.Max( 1.5f * Utils.GUIHelper.EditorSkin.label.CalcSize( WindowTitle ).x, Utils.GUIHelper.EditorSkin.button.CalcSize( GetGUIContent( Manager.MouseOverObject ) ).x );

        // Adding "null" if click in "space".
        m_gameObjectsToChoose.Add( Manager.MouseOverObject );

        if ( Manager.MouseOverObject != null ) {
          Transform parent = Manager.MouseOverObject.transform.parent;
          while ( parent != null ) {
            buttonMaxWidth = Mathf.Max( buttonMaxWidth, Utils.GUIHelper.EditorSkin.button.CalcSize( GetGUIContent( parent.gameObject ) ).x );

            m_gameObjectsToChoose.Add( parent.gameObject );
            parent = parent.parent;
          }
        }

        if ( SceneViewWindow.GetWindowData( OnMultipleOptions ) != null )
          Debug.LogError( "Window already open!" );

        var data = SceneViewWindow.Show( OnMultipleOptions, new Vector2( buttonMaxWidth, 0 ), Event.current.mousePosition + new Vector2( -0.5f * buttonMaxWidth, -10 ), WindowTitle.text );
        data.Movable = true;
      }

      if ( m_objectSelected ) {
        FireOnSelected( m_selected );
        Clear();
      }
    }

    private void OnMultipleOptions( EventType eventType )
    {
      // Note: gameObject may be null if click in "space".
      foreach ( var gameObject in m_gameObjectsToChoose ) {
        if ( GUILayout.Button( GetGUIContent( gameObject ), Utils.GUIHelper.EditorSkin.button ) ) {
          m_selected = gameObject;
          m_objectSelected = true;
        }
      }
    }

    private void FireOnSelected( GameObject gameObject )
    {
      m_onSelectedCallback( gameObject );
    }
  }
}
