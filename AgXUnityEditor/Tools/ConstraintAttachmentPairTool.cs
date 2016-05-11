using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;

namespace AgXUnityEditor.Tools
{
  public class ConstraintAttachmentPairTool : Tool
  {
    public enum ToolMode
    {
      SelectObjects,
      FindEdge
    }

    public enum ESelectObjectsState
    {
      Inactive,
      SelectFirst,
      SelectSecond,
      Done
    }

    public ConstraintAttachmentPair AttachmentPair { get; private set; }

    public ToolMode Mode { get; private set; }

    public ESelectObjectsState SelectObjectsState { get; private set; }

    public ConstraintAttachmentPairTool( ConstraintAttachmentPair attachmentPair, ToolMode mode )
    {
      AttachmentPair = attachmentPair;
      Mode = mode;

      SceneViewWindow.Show( OnWindowGUI, new Vector2( 400, 42 ), new Vector2( 2, 22 ), "AgXUnity Constraint Attachment Tool" );

      if ( Mode == ToolMode.SelectObjects ) {
        SelectObjectsState = ESelectObjectsState.SelectFirst;
        AddChild( new SelectGameObjectTool( OnSelectedCallback ) );
      }
      else if ( Mode == ToolMode.FindEdge ) {
        AddChild( new EdgeDetectionTool( OnEdgeClickCallback ) );
      }
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      OnSceneViewGUIChildren( sceneView );

      if ( Mode == ToolMode.SelectObjects )
        HandleOnSelectMode();
      else if ( Mode == ToolMode.FindEdge )
        HandleFindEdgeMode();

      if ( Mode == ToolMode.FindEdge )
        AttachmentPair.Update();
    }

    public override void OnRemove()
    {
      SceneViewWindow.Close( OnWindowGUI );
    }

    private void HandleOnSelectMode()
    {
      SelectGameObjectTool tool = GetChild<SelectGameObjectTool>();
      if ( tool == null || SelectObjectsState == ESelectObjectsState.Done )
        PerformRemoveFromParent();
      else if ( Manager.KeyEscapeDown ) {
        SelectObjectsState = SelectObjectsState + 1;
        tool.Clear();
      }
    }

    private void OnSelectedCallback( GameObject gameObject )
    {
      if ( SelectObjectsState == ESelectObjectsState.SelectFirst ) {
        if ( gameObject == null || gameObject.GetComponentInParent<RigidBody>() == null ) {
          Debug.LogWarning( "Reference object in a constraint must contain a rigid body component.\nSelected object ignored.", gameObject );
          return;
        }

        if ( !Manager.KeyEscapeDown )
          AttachmentPair.ReferenceObject = gameObject;
        SelectObjectsState = ESelectObjectsState.SelectSecond;
      }
      else if ( SelectObjectsState == ESelectObjectsState.SelectSecond ) {
        if ( gameObject != null && AttachmentPair.ReferenceObject != null && AttachmentPair.ReferenceObject.GetComponentInParent<RigidBody>() == gameObject.GetComponentInParent<RigidBody>() ) {
          Debug.LogWarning( "Reference and connected game object shares the same parent rigid body - invalid configuration.\nSelected object ignored.", gameObject );
          return;
        }

        if ( !Manager.KeyEscapeDown )
          AttachmentPair.ConnectedObject = gameObject;
        SelectObjectsState = ESelectObjectsState.Done;
      }
    }

    private void HandleFindEdgeMode()
    {
      EdgeDetectionTool edgeTool = GetChild<EdgeDetectionTool>();
      FrameTool frameTool = GetChild<FrameTool>();
      if ( ( frameTool == null && edgeTool == null ) || Manager.KeyEscapeDown ) {
        PerformRemoveFromParent();
        return;
      }

      if ( edgeTool != null && edgeTool.Target != Manager.MouseOverObject && Manager.MouseOverObjectIncludingHidden == Manager.MouseOverObject && Manager.HijackLeftMouseClick() )
        edgeTool.Target = Manager.MouseOverObject;
    }

    private void OnEdgeClickCallback( EdgeDetectionTool.Result result )
    {
      if ( result.Edge == null )
        return;

      FrameTool frameTool = GetChild<FrameTool>();
      if ( frameTool != null )
        RemoveChild( frameTool );

      Vector3 worldCenter      = 0.5f * ( result.Edge.Start + result.Edge.End );
      Quaternion worldRotation = Quaternion.LookRotation( result.Edge.Direction, result.Edge.Normal );

      AttachmentPair.ReferenceFrame.Position = worldCenter;
      AttachmentPair.ReferenceFrame.Rotation = worldRotation;

      AttachmentPair.ConnectedFrame.Position = worldCenter;
      AttachmentPair.ConnectedFrame.Rotation = worldRotation;

      AddChild( new FrameTool( AttachmentPair.ReferenceFrame ) );
      RemoveChild( GetChild<EdgeDetectionTool>() );

      Manager.ActivateTool( new Manager.ToolData() { Tool = new FrameTool( AttachmentPair.ReferenceFrame ) } );
    }

    private void OnWindowGUI( EventType eventType )
    {
      if ( Mode == ToolMode.SelectObjects ) {
        if ( SelectObjectsState == ESelectObjectsState.SelectFirst )
          GUILayout.Label( Utils.GUIHelper.MakeLabel( "Pick object in scene view to be the <b>reference</b> object." ), Utils.GUIHelper.EditorSkin.label );
        else if ( SelectObjectsState == ESelectObjectsState.SelectSecond)
          GUILayout.Label( Utils.GUIHelper.MakeLabel( "Pick object in scene view to be the <b>connected</b> object." ), Utils.GUIHelper.EditorSkin.label );
      }
    }
  }
}
