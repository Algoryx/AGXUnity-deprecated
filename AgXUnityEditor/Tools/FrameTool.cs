using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class FrameTool : Tool
  {
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
      sceneView.Focus();

      if ( Frame == null )
        return;

      if ( GetParent() == null && Manager.KeyEscapeDown ) {
        PerformRemoveFromParent();
        return;
      }

      // Shows position handle if, e.g., scale or some other strange setting is used in the editor.
      bool isRotation = UnityEditor.Tools.current == UnityEditor.Tool.Rotate;
      if ( !isRotation )
        Frame.Position = PositionTool( Frame.Position, Frame.Rotation, Size, Alpha );
      else
        Frame.Rotation = RotationTool( Frame.Position, Frame.Rotation, Size, Alpha );
    }
  }
}
