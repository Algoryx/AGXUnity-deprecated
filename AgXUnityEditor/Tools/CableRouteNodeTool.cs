using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class CableRouteNodeTool : Tool
  {
    public Cable Cable { get; private set; }

    public CableTool CableTool { get { return GetParent() as CableTool; } }

    public CableRouteNode Node { get; private set; }

    public FrameTool FrameTool
    {
      get { return GetChild<FrameTool>(); }
    }

    public Utils.VisualPrimitiveSphere Visual { get { return GetOrCreateVisualPrimitive<Utils.VisualPrimitiveSphere>( "cableRouteNode" ); } }

    public bool Selected
    {
      get { return CableTool.Selected == Node; }
      set { CableTool.Selected = value ? Node : null; }
    }

    public CableRouteNodeTool( CableRouteNode node, Cable cable )
    {
      Node = node;
      Cable = cable;
      AddChild( new FrameTool( node.Frame ) { OnChangeDirtyTarget = Cable, TransformHandleActive = false } );

      Visual.Color = Color.yellow;
      Visual.MouseOverColor = new Color( 0.1f, 0.96f, 0.15f, 1.0f );
      Visual.OnMouseClick += OnClick;
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( Cable == null || Node == null || !Cable.Route.Contains( Node ) ) {
        PerformRemoveFromParent();
        return;
      }

      float radius = 3f * Cable.Radius;
      Visual.Visible = !EditorApplication.isPlaying;
      Visual.Color = Selected ? Visual.MouseOverColor : Color.yellow;
      Visual.SetTransform( Node.Frame.Position, Node.Frame.Rotation, radius, true, 1.2f * Cable.Radius, Mathf.Max( 1.5f * Cable.Radius, 0.25f ) );
    }

    private void OnClick( AgXUnity.Utils.Raycast.Hit hit, Utils.VisualPrimitive primitive )
    {
      Selected = true;
    }
  }
}
