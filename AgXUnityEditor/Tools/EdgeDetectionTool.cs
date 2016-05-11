using System;
using UnityEditor;
using UnityEngine;
using AgXUnity.Collide;
using AgXUnity.Utils;

namespace AgXUnityEditor.Tools
{
  public class EdgeDetectionTool : Tool
  {
    public class Result
    {
      private GameObject m_target = null;
      private MeshUtils.FindTriangleResult m_cachedResult = null;

      public MeshUtils.Edge Edge { get; set; }

      public GameObject Target
      {
        get { return m_target; }
        set
        {
          m_cachedResult = null;
          Edge           = null;
          m_target       = value;
        }
      }

      public MeshUtils.Edge FindEdgeOnTarget( Ray ray, float rayLength = 500.0f )
      {
        Shape shape = Target.GetComponent<Shape>();
        AgXUnity.Collide.Mesh mesh = shape as AgXUnity.Collide.Mesh;

        if ( mesh != null ) {
          m_cachedResult = MeshUtils.FindClosestTriangle( mesh.SourceObject, shape.gameObject, ray, rayLength, m_cachedResult );
          if ( m_cachedResult.Valid )
            Edge = ShapeUtils.FindClosestEdge( ray, rayLength, m_cachedResult.WorldEdges );
        }
        else if ( shape != null ) {
          ShapeUtils utils = shape.GetUtils();
          if ( utils != null )
            Edge = utils.FindClosestEdge( ray, rayLength, 2.0f );
        }
        else {
        }

        return Edge;
      }
    }

    private Utils.VisualPrimitiveCylinder EdgeVisual { get { return GetOrCreateVisualPrimitive<Utils.VisualPrimitiveCylinder>( "edgeVisual" ); } }
    private Result m_currentResult = new Result();
    private Action<Result> m_onEdgeClickCallback = delegate { };

    public GameObject Target
    {
      get { return m_currentResult.Target; }
      set { m_currentResult.Target = value; }
    }

    public Result CurrentResult { get { return m_currentResult; } }

    public EdgeDetectionTool( Action<Result> onEdgeClickCallback = null )
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

      if ( m_currentResult.FindEdgeOnTarget( HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ) ) != null ) {
        EdgeVisual.Visible = true;
        EdgeVisual.Color = m_currentResult.Edge.Type == MeshUtils.Edge.EdgeType.Triangle ? Color.yellow : Color.red;
        EdgeVisual.SetTransform( m_currentResult.Edge.Start, m_currentResult.Edge.End, 0.045f );
      }
    }

    private void OnEdgeClick( Utils.VisualPrimitive primitive )
    {
      m_onEdgeClickCallback( m_currentResult );
    }
  }
}
