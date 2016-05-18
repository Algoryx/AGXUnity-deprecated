using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class FrameTool : Tool
  {
    public static FrameTool FindActive( Frame frame )
    {
      return FindActive( Manager.GetActiveTool(), frame );
    }

    private static FrameTool FindActive( Tool tool, Frame frame )
    {
      if ( tool == null )
        return null;

      FrameTool frameTool = tool as FrameTool;
      if ( frameTool != null && frameTool.Frame == frame )
        return frameTool;

      foreach ( Tool child in tool.GetChildren() ) {
        frameTool = FindActive( child, frame );
        if ( frameTool != null )
          return frameTool;
      }

      return null;
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

    public void Remove()
    {
      PerformRemoveFromParent();
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
      if ( !isRotation )
        Frame.Position = PositionTool( Frame.Position, Frame.Rotation, Size, Alpha );
      else
        Frame.Rotation = RotationTool( Frame.Position, Frame.Rotation, Size, Alpha );
    }
  }
}
