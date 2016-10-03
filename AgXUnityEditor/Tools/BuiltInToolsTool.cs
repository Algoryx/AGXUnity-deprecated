using System;
using UnityEngine;
using UnityEditor;

namespace AgXUnityEditor.Tools
{
  public class BuiltInToolsTool : Tool
  {
    public SelectGameObjectTool SelectGameObject
    {
      get { return GetChild<SelectGameObjectTool>(); }
      private set
      {
        if ( SelectGameObject != null )
          SelectGameObject.PerformRemoveFromParent();

        if ( value != null ) {
          value.MenuTool.RemoveOnClickMiss = true;
          AddChild( value );
        }
      }
    }

    public Utils.KeyHandler SelectGameObjectKey { get { return GetKeyHandler( "SelectObject" ); } }

    public PickHandlerTool PickHandler
    {
      get { return GetChild<PickHandlerTool>(); }
      set
      {
        if ( PickHandler != null )
          PickHandler.PerformRemoveFromParent();

        if ( value != null )
          AddChild( value );
      }
    }

    public Utils.KeyHandler PickHandlerKey { get { return GetKeyHandler( "PickHandler" ); } }

    public BuiltInToolsTool()
    {
      AddKeyHandler( "SelectObject", new Utils.KeyHandler( KeyCode.S ) );
      AddKeyHandler( "PickHandler", new Utils.KeyHandler( KeyCode.A ) );
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      var currentEvent = Event.current;

      HandleSceneViewSelectTool( currentEvent, sceneView );
      HandlePickHandler( currentEvent, sceneView );
    }

    private void HandleSceneViewSelectTool( Event current, SceneView sceneView )
    {
      bool isKeyS = SelectGameObjectKey.IsDown &&
                    EditorWindow.mouseOverWindow == sceneView &&
                    !current.control &&
                    !current.shift &&
                    !current.alt;
      if ( !isKeyS )
        return;

      SelectGameObjectTool selectTool = SelectGameObject;
      if ( selectTool == null && current.type == EventType.KeyDown )
        SelectGameObject = new SelectGameObjectTool() { OnSelect = go => { Selection.activeGameObject = go; } };
      else if ( selectTool != null && !selectTool.SelectionWindowActive && current.type == EventType.KeyUp )
        SelectGameObject = null;
    }

    private void HandlePickHandler( Event current, SceneView sceneView )
    {
      bool activatePickHandler = EditorApplication.isPlaying &&
                                 PickHandler == null &&
                                 EditorWindow.mouseOverWindow == sceneView &&
                                 PickHandlerKey.IsDown &&
                                 !current.shift &&
                                 !current.alt &&
                                 current.type == EventType.MouseDown &&
                                 current.button >= 0 &&
                                 current.button <= 2;

      if ( activatePickHandler ) {
        Predicate<Event> removePredicate = null;
        AgXUnity.PickHandler.DofTypes dofTypes = AgXUnity.PickHandler.DofTypes.Translation;

        // Left mouse button = ball joint.
        if ( current.button == 0 ) {
          // If left mouse - make sure the manager is taking over this mouse event.
          Manager.HijackLeftMouseClick();

          removePredicate = ( e ) => { return Manager.HijackLeftMouseClick(); };
          // Ball joint.
          dofTypes = AgXUnity.PickHandler.DofTypes.Translation;
        }
        // Middle/scroll mouse button = lock joint.
        else if ( current.button == 2 ) {
          current.Use();

          removePredicate = ( e ) => { return e.type == EventType.MouseUp && e.button == 2; };
          // Lock joint.
          dofTypes = AgXUnity.PickHandler.DofTypes.Translation | AgXUnity.PickHandler.DofTypes.Rotation;
        }
        // Right mouse button = angular lock?
        else {
          Debug.Assert( current.button == 1 );

          current.Use();

          removePredicate = ( e ) => { return e.type == EventType.MouseUp && e.button == 1; };
          // Angular lock.
          dofTypes = AgXUnity.PickHandler.DofTypes.Rotation;
        }

        PickHandler = new PickHandlerTool( dofTypes, removePredicate );
      }
    }
  }
}
