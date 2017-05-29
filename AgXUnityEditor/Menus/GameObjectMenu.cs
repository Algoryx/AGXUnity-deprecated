using System;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;
using AgXUnity.Rendering;
using AgXUnity.Collide;

namespace AgXUnityEditor.Menus
{
  public static class GameObjectMenu
  {
    [MenuItem( "GameObject/AgXUnity/Create visual" )]
    public static void CreateVisual()
    {
      if ( Selection.activeGameObject == null || AssetDatabase.Contains( Selection.activeGameObject ) )
        return;

      Undo.SetCurrentGroupName( "Create GameObject shape visual." );
      var grouId = Undo.GetCurrentGroup();

      var shapes = Selection.activeGameObject.GetComponentsInChildren<Shape>();
      foreach ( var shape in shapes ) {
        if ( !ShapeVisual.HasShapeVisual( shape ) ) {
          var go = ShapeVisual.Create( shape );
          if ( go != null )
            Undo.RegisterCreatedObjectUndo( go, "Shape visual" );
        }
      }

      Undo.CollapseUndoOperations( grouId );
    }
  }
}
