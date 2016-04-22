﻿using UnityEngine;
using UnityEditor;
using AgXUnity.Collide;
using AgXUnity.Utils;

namespace AgXUnityEditor.Tools
{
  /// <summary>
  /// Tool to resize supported primitive shape types. Supported types
  /// (normally Box, Capsule, Cylinder and Sphere) has ShapeUtils set.
  /// 
  /// This tool is active when ActiveKey is down and a supported shape
  /// is selected. For symmetric resize, both SymmetricScaleKey and
  /// ActiveKey has to be down.
  /// </summary>
  public class ShapeResizeTool : Tool
  {
    /// <summary>
    /// Key code to activate this tool.
    /// </summary>
    public Utils.GUIHelper.KeyHandler ActivateKey { get; set; }

    /// <summary>
    /// Key code for symmetric scale/resize.
    /// </summary>
    public Utils.GUIHelper.KeyHandler SymmetricScaleKey { get; set; }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( Selection.activeGameObject == null )
        return;

      Shape shape = Selection.activeGameObject.GetComponent<Shape>();
      if ( shape == null )
        return;

      if ( ActivateKey.IsDown )
        Update( shape, SymmetricScaleKey.IsDown );
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
            shape.transform.position += shape.transform.TransformDirection( localPositionDelta );
        }
      }
    }
  }
}
