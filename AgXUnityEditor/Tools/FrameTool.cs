using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class FrameTool : Tool
  {
    public static FrameTool FindActive( Frame frame )
    {
      return FindActive<FrameTool>( frameTool => frameTool.Frame == frame );
    }

    /// <summary>
    /// Frame this tool controls.
    /// </summary>
    public Frame Frame { get; set; }

    /// <summary>
    /// Size/Scale of this tool, if 1.0f it'll be the size of the default
    /// Unity position/rotation handle.
    /// </summary>
    public float Size { get; set; }

    /// <summary>
    /// Transparency value, default 1.0f.
    /// </summary>
    public float Alpha { get; set; }

    public FrameTool( Frame frame, float size = 0.6f, float alpha = 1.0f )
    {
      Frame = frame;
      Size  = size;
      Alpha = alpha;
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( Frame == null )
        return;

      if ( GetParent() == null && Manager.KeyEscapeDown ) {
        PerformRemoveFromParent();
        return;
      }

      // Shows position handle if, e.g., scale or some other strange setting is used in the editor.
      bool isRotation = UnityEditor.Tools.current == UnityEditor.Tool.Rotate;

      Undo.RecordObject( Frame, "FrameTool" );

      // NOTE: Checking GUI changes before updating position/rotation to avoid
      //       drift in the values.
      if ( !isRotation ) {
        var newPosition = PositionTool( Frame.Position, Frame.Rotation, Size, Alpha );
        if ( Vector3.SqrMagnitude( Frame.Position - newPosition) > 1.0E-6f )
          Frame.Position = newPosition;
      }
      else {
        var newRotation = RotationTool( Frame.Position, Frame.Rotation, Size, Alpha );
        if ( ( Quaternion.Inverse( Frame.Rotation ) * newRotation ).eulerAngles.sqrMagnitude > 1.0E-6f )
          Frame.Rotation = newRotation;
      }
    }
  }
}
