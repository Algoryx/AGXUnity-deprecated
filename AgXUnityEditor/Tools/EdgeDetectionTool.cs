using System;
using UnityEditor;
using UnityEngine;
using AgXUnity.Collide;
using AgXUnity.Utils;

namespace AgXUnityEditor.Tools
{
  public class EdgeDetectionTool : Tool
  {
    private Utils.VisualPrimitiveCylinder EdgeVisual { get { return GetOrCreateVisualPrimitive<Utils.VisualPrimitiveCylinder>( "edgeVisual" ); } }
    private Raycast m_raycast = new Raycast();
    private Action<Raycast.ClosestEdgeHit> m_onEdgeClickCallback = delegate { };

    public GameObject Target
    {
      get { return m_raycast.Target; }
      set { m_raycast.Target = value; }
    }

    public EdgeDetectionTool( Action<Raycast.ClosestEdgeHit> onEdgeClickCallback = null )
    {
      EdgeVisual.OnMouseClick += OnEdgeClick;
      m_onEdgeClickCallback += onEdgeClickCallback;
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( EdgeVisual.MouseOver )
        return;

      EdgeVisual.Visible = false;

      if ( Target == null )
        return;

      if ( m_raycast.Test( HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ) ).Valid ) {
        EdgeVisual.Visible = true;
        EdgeVisual.Color = m_raycast.LastHit.ClosestEdge.Edge.Type == MeshUtils.Edge.EdgeType.Triangle ? Color.yellow : Color.red;
        EdgeVisual.SetTransform( m_raycast.LastHit.ClosestEdge.Edge.Start, m_raycast.LastHit.ClosestEdge.Edge.End, 0.045f );
      }
    }

    private void OnEdgeClick( Utils.VisualPrimitive primitive )
    {
      m_onEdgeClickCallback( m_raycast.LastHit.ClosestEdge );
    }
  }
}
