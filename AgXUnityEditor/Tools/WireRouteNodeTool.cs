using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class WireRouteNodeTool : Tool
  {
    public static WireRouteNodeTool GetRouteNodeTool( WireRouteNode node )
    {
      if ( node == null )
        return null;

      return node.GetEditorData<WireRouteNodeTool>();
    }

    public WireRouteNode Node { get; private set; }

    public Utils.VisualPrimitiveSphere Visual { get { return GetOrCreateVisualPrimitive<Utils.VisualPrimitiveSphere>( "rn" ); } }

    private bool m_selected = false;
    public bool Selected
    {
      get { return m_selected; }
      set
      {
        if ( m_selected == value )
          return;

        if ( GetParent() != null ) {
          WireRouteNodeTool[] allRnTools = GetParent().GetChildren<WireRouteNodeTool>();
          foreach ( WireRouteNodeTool rnTool in allRnTools ) {
            rnTool.RemoveChild( rnTool.GetChild<FrameTool>() );
            rnTool.m_selected = false;
          }
        }

        m_selected = value;

        if ( m_selected )
          AddChild( new FrameTool( Node.Frame ) { OnChangeDirtyTarget = Node.Wire } );
      }
    }

    public WireRouteNodeTool( WireRouteNode node )
    {
      Node = node;
      Node.SetEditorData( this );
      Visual.Color = Visual.MouseOverColor = new Color( 0.1f, 0.96f, 0.15f, 1.0f );
      Visual.OnMouseClick += OnClick;
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( Node.Wire == null ) {
        Node.SetEditorData( null );
        PerformRemoveFromParent();
        return;
      }

      OnSceneViewGUIChildren( sceneView );

      float radius = 3f * Node.Wire.Radius;

      Visual.Visible = !EditorApplication.isPlaying;
      Visual.SetTransform( Node.Frame.Position, Node.Frame.Rotation, radius, true, 2f * Node.Wire.Radius, Mathf.Max( 1.5f * Node.Wire.Radius, 0.25f ) );
    }

    private void OnClick( Utils.VisualPrimitive primitive )
    {
      Selected = true;
    }
  }
}
