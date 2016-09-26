using System;
using UnityEngine;
using UnityEditor;

namespace AgXUnityEditor.Tools
{
  public class BuiltInToolsTool : Tool
  {
    public SelectGameObjectTool SceneViewSelectTool
    {
      get { return GetChild<SelectGameObjectTool>(); }
      private set
      {
        if ( SceneViewSelectTool != null )
          SceneViewSelectTool.PerformRemoveFromParent();

        if ( value != null ) {
          value.MenuTool.RemoveOnClickMiss = true;
          AddChild( value );
        }
      }
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      var currentEvent = Event.current;

      HandleSceneViewSelectTool( currentEvent, sceneView );
    }

    private void HandleSceneViewSelectTool( Event current, SceneView sceneView )
    {
      bool isKeyS = current.isKey && current.keyCode == KeyCode.S && !current.control && !current.shift && !current.alt;
      if ( !isKeyS )
        return;

      SelectGameObjectTool selectTool = SceneViewSelectTool;
      if ( selectTool == null && current.type == EventType.KeyDown )
        SceneViewSelectTool = new SelectGameObjectTool() { OnSelect = go => { Selection.activeGameObject = go; } };
      else if ( selectTool != null && !selectTool.SelectionWindowActive && current.type == EventType.KeyUp )
        SceneViewSelectTool = null;
    }
  }
}
