using UnityEngine;
using UnityEditor;
using AgXUnity.Collide;

namespace AgXUnityEditor.Tools
{
  public class ShapeResizeTool : Tool
  {
    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( Selection.activeGameObject == null )
        return;

      Shape shape = Selection.activeGameObject.GetComponent<Shape>();
      if ( shape == null )
        return;

      // Tool activated on control down.
      // Symmetric mode if ctrl + shift is down.
      bool activateKeyDown = Event.current.control;
      if ( activateKeyDown )
        Update( shape, Event.current.shift );
    }

    private void Update( Shape shape, bool symmetricScale )
    {
      if ( shape == null )
        return;

      ShapeUtils utils = shape.GetUtils();
      if ( utils == null )
        return;

      Undo.RecordObject( shape, "ShapeResizeTool" );
      Undo.RecordObject( shape.transform, "ShapeResizeToolTransform" );

      Color color = Color.gray;
      float scale = 0.35f;
      foreach ( ShapeUtils.Direction dir in System.Enum.GetValues( typeof( ShapeUtils.Direction ) ) ) {
        Vector3 delta = DeltaSliderTool( utils.GetWorldFace( dir ), utils.GetWorldFaceDirection( dir ), color, scale );
        if ( delta.magnitude > 1.0E-5f ) {
          Vector3 localSizeChange = shape.transform.InverseTransformDirection( delta );
          Vector3 localPositionDelta = 0.5f * localSizeChange;
          if ( !symmetricScale && utils.IsHalfSize( dir ) )
            localSizeChange *= 0.5f;

          utils.UpdateSize( localSizeChange, dir );

          if ( !symmetricScale )
            shape.transform.position += shape.transform.TransformVector( localPositionDelta );
        }
      }
    }
  }
}
