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

    public Utils.KeyHandler SelectGameObjectKeyHandler { get { return EditorSettings.Instance.BuiltInToolsTool_SelectGameObjectKeyHandler; } }

    public Utils.KeyHandler SelectRigidBodyKeyHandler { get { return EditorSettings.Instance.BuiltInToolsTool_SelectRigidBodyKeyHandler; } }

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

    public Utils.KeyHandler PickHandlerKeyHandler { get { return EditorSettings.Instance.BuiltInToolsTool_PickHandlerKeyHandler; } }

    public bool SelectGameObjectTrigger( Event current, SceneView sceneView )
    {
      return SelectGameObjectKeyHandler.IsDown &&
             EditorWindow.mouseOverWindow == sceneView &&
            !current.control &&
            !current.shift &&
            !current.alt;
    }

    public bool SelectRigidBodyTrigger( Event current, SceneView sceneView )
    {
      return SelectRigidBodyKeyHandler.IsDown &&
             EditorWindow.mouseOverWindow == sceneView &&
            !current.control &&
            !current.shift &&
            !current.alt;
    }

    public bool PickHandlerTrigger( Event current, SceneView sceneView )
    {
      return EditorApplication.isPlaying &&
             PickHandler == null &&
             EditorWindow.mouseOverWindow == sceneView &&
             PickHandlerKeyHandler.IsDown &&
            !current.shift &&
            !current.alt &&
             current.type == EventType.MouseDown &&
             current.button >= 0 &&
             current.button <= 2;
    }

    public BuiltInToolsTool()
    {
      AddKeyHandler( "SelectObject", SelectGameObjectKeyHandler );
      AddKeyHandler( "SelectRigidBody", SelectRigidBodyKeyHandler );
      AddKeyHandler( "PickHandler", PickHandlerKeyHandler );
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      var currentEvent = Event.current;

      HandleSceneViewSelectTool( currentEvent, sceneView );
      HandlePickHandler( currentEvent, sceneView );
    }

    private void HandleSceneViewSelectTool( Event current, SceneView sceneView )
    {
      // TODO: Add keys to select body, shape etc.

      // Routes each selected object to its correct selection.
      // Assigning 'selectedObjects' to 'Selection.objects' doesn't
      // trigger onSelectionChanged (desired behavior).
      UnityEngine.Object[] selectedObjects = Selection.objects;
      bool selectRigidBodyMode = SelectRigidBodyTrigger( current, sceneView );
      for ( int i = 0; i < selectedObjects.Length; ++i ) {
        // TODO: Key combo to select bodies etc.
        UnityEngine.Object routedObject = Manager.RouteObject( selectedObjects[ i ] );
        AgXUnity.RigidBody rigidBody = selectRigidBodyMode &&
                                       routedObject != null &&
                                       routedObject is GameObject ?
                                         ( routedObject as GameObject ).GetComponentInParent<AgXUnity.RigidBody>() :
                                         null;
        selectedObjects[ i ] = rigidBody != null ? rigidBody.gameObject : routedObject;
      }
      Selection.objects = selectedObjects;

      if ( SelectGameObjectTrigger( current, sceneView ) ) {
        // User is holding activate select tool - SelectGameObjectTool is waiting for the mouse click.
        if ( SelectGameObject == null )
          SelectGameObject = new SelectGameObjectTool() { OnSelect = go => { Selection.activeGameObject = go; } };
      }
      // The user has released select game object trigger and the window isn't showing.
      else if ( SelectGameObject != null && !SelectGameObject.SelectionWindowActive )
        SelectGameObject = null;
    }

    private void HandlePickHandler( Event current, SceneView sceneView )
    {
      if ( !PickHandlerTrigger( current, sceneView ) )
        return;

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
