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
        if ( GetParent() != null ) {
          WireRouteNodeTool[] allRnTools = GetParent().GetChildren<WireRouteNodeTool>();
          foreach ( WireRouteNodeTool rnTool in allRnTools ) {
            rnTool.RemoveChild( rnTool.GetChild<FrameTool>() );
            rnTool.m_selected = false;
          }
        }

        m_selected = value;

        if ( m_selected )
          AddChild( new FrameTool( Node.Frame ) );
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

      Visual.Visible = true;
      Visual.SetTransform( Node.Frame.Position, Node.Frame.Rotation, radius, true, 2f * Node.Wire.Radius, Mathf.Max( 1.5f * Node.Wire.Radius, 0.25f ) );
    }

    public override void OnInspectorGUI( GUISkin skin )
    {
      EditorGUILayout.BeginHorizontal();
      GUILayout.Label( Utils.GUI.MakeLabel( "Tools:", true ), skin.label );
      GUILayout.FlexibleSpace();
      if ( GUILayout.Button( Utils.GUI.MakeLabel( "Find point on object" ) ) ) {
        RemoveChild( GetChild<FindPointTool>() );
        AddChild( new FindPointTool() { RemoveSelfWhenDone = true, OnNewPointData = OnNewPointData } );
      }
      EditorGUILayout.EndHorizontal();
    }

    private void OnClick( Utils.VisualPrimitive primitive )
    {
      Selected = true;
    }

    private void OnNewPointData( FindPointTool.PointData pointData )
    {
      Node.Frame.SetParent( pointData.Parent );
      Node.Frame.Position = pointData.WorldPosition;
      Node.Frame.Rotation = pointData.WorldRotation;
    }
  }
}
