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

    /// <summary>
    /// Enable/disable SelectGameObjectTool to set frame parent from picking
    /// in scene view.
    /// </summary>
    public bool SelectParent
    {
      get { return GetChild<SelectGameObjectTool>() != null; }
      set
      {
        if ( value && GetChild<SelectGameObjectTool>() == null ) {
          RemoveAllChildren();
          SelectGameObjectTool selectGameObjectTool = new SelectGameObjectTool();
          selectGameObjectTool.OnSelect += parent =>
          {
            Frame.SetParent( parent );
            RemoveChild( GetChild<SelectGameObjectTool>() );
            DirtyTarget();
          };
          AddChild( selectGameObjectTool );
        }
        else if ( !value )
          RemoveChild( GetChild<SelectGameObjectTool>() );
      }
    }

    public bool FindTransformGivenPointOnSurface
    {
      get { return GetChild<FindPointTool>() != null; }
      set
      {
        if ( value && GetChild<FindPointTool>() == null ) {
          RemoveAllChildren();
          FindPointTool pointTool = new FindPointTool();
          pointTool.OnPointFound = data =>
          {
            Frame.SetParent( data.Target );
            Frame.Position = data.Triangle.Point;
            Frame.Rotation = FindRotationGivenParentTool( data.Triangle.ClosestEdge.Direction, data.Triangle.Normal );

            DirtyTarget();
          };
          AddChild( pointTool );
        }
        else if ( !value )
          RemoveChild( GetChild<FindPointTool>() );
      }
    }

    public bool FindTransformGivenEdge
    {
      get { return GetChild<EdgeDetectionTool>() != null; }
      set
      {
        if ( value && GetChild<EdgeDetectionTool>() == null ) {
          RemoveAllChildren();
          EdgeDetectionTool edgeTool = new EdgeDetectionTool();
          edgeTool.OnEdgeFound += data =>
          {
            Frame.SetParent( data.Target );
            Frame.Position = 0.5f * ( data.EdgeData.Edge.Start + data.EdgeData.Edge.End );
            Frame.Rotation = FindRotationGivenParentTool( data.EdgeData.Edge.Direction, data.EdgeData.Edge.Normal );

            DirtyTarget();
          };

          AddChild( edgeTool );
        }
        else if ( !value )
          RemoveChild( GetChild<EdgeDetectionTool>() );
      }
    }

    /// <summary>
    /// True if all tools related to the frame should be active. Default true.
    /// </summary>
    public bool RepositionToolsActive { get; set; }

    /// <summary>
    /// When the position/rotation has been changed and this property is
    /// set the update method will call EditorUtility.SetDirty( OnChangeDirtyTarget )
    /// to force update of GUI related to this object.
    /// </summary>
    public UnityEngine.Object OnChangeDirtyTarget { get; set; }

    public FrameTool( Frame frame, float size = 0.6f, float alpha = 1.0f )
    {
      Frame = frame;
      Size  = size;
      Alpha = alpha;
      RepositionToolsActive = true;
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( Frame == null )
        return;

      OnSceneViewGUIChildren( sceneView );

      if ( GetParent() == null && Manager.KeyEscapeDown ) {
        PerformRemoveFromParent();
        return;
      }

      Undo.RecordObject( Frame, "FrameTool" );

      if ( !RepositionToolsActive )
        return;

      // Shows position handle if, e.g., scale or some other strange setting is used in the editor.
      bool isRotation = UnityEditor.Tools.current == UnityEditor.Tool.Rotate;

      // NOTE: Checking GUI changes before updating position/rotation to avoid
      //       drift in the values.
      bool changesMade = false;
      if ( !isRotation ) {
        var newPosition = PositionTool( Frame.Position, Frame.Rotation, Size, Alpha );
        changesMade = Vector3.SqrMagnitude( Frame.Position - newPosition ) > 1.0E-6f;
        if ( changesMade )
          Frame.Position = newPosition;
      }
      else {
        var newRotation = RotationTool( Frame.Position, Frame.Rotation, Size, Alpha );
        changesMade = ( Quaternion.Inverse( Frame.Rotation ) * newRotation ).eulerAngles.sqrMagnitude > 1.0E-6f;
        if ( changesMade )
          Frame.Rotation = newRotation;
      }

      if ( changesMade )
        DirtyTarget();
    }

    private void DirtyTarget()
    {
      if ( OnChangeDirtyTarget != null )
        EditorUtility.SetDirty( OnChangeDirtyTarget );
    }

    /// <summary>
    /// Wire tools normally desires forward to be the normal. Constraint tools normally
    /// desires forward to be the edge.
    /// </summary>
    /// <param name="edgeDirection">Direction of the closest edge.</param>
    /// <param name="normal">Normal to surface/edge.</param>
    /// <returns>Rotation with either normal as forward (wires) or edge (constraints).</returns>
    private Quaternion FindRotationGivenParentTool( Vector3 edgeDirection, Vector3 normal )
    {
      if ( GetActiveTool<WireTool>() != null )
        return Quaternion.LookRotation( normal, edgeDirection );
      else
        return Quaternion.LookRotation( edgeDirection, normal );
    }
  }
}
