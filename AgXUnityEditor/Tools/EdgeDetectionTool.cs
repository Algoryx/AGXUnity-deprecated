using UnityEditor;
using UnityEngine;
using AgXUnity.Collide;
using AgXUnity.Utils;

namespace AgXUnityEditor.Tools
{
  public class EdgeDetectionTool : Tool
  {
    private Utils.VisualPrimitiveCylinder m_edgeVisual = new Utils.VisualPrimitiveCylinder( "Unlit/Color" );

    private GameObject m_target = null;
    public GameObject Target
    {
      get { return m_target; }
      set { m_target = value; }
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( m_edgeVisual.MouseOver )
        return;

      m_edgeVisual.Visible = false;

      if ( Target == null )
        return;

      MeshUtils.Edge edge = FindEdgeOnTarget();
      if ( edge != null ) {
        m_edgeVisual.Visible = true;
        m_edgeVisual.SetTransform( edge.Start, edge.End, 0.045f );
      }
    }

    private MeshUtils.Edge FindEdgeOnTarget()
    {
      Shape shape = Target.GetComponent<Shape>();
      AgXUnity.Collide.Mesh mesh = shape as AgXUnity.Collide.Mesh;

      MeshUtils.Edge edge = null;
      Ray camRay = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
      float camRayLength = 500.0f;
      if ( mesh != null ) {
        MeshUtils.FindTriangleResult meshResult = MeshUtils.FindClosestTriangle( mesh.SourceObject, shape.gameObject, camRay, camRayLength );
        if ( meshResult.Valid )
          edge = ShapeUtils.FindClosestEdge( camRay, camRayLength, meshResult.WorldEdges );
      }
      else if ( shape != null ) {
        ShapeUtils utils = shape.GetUtils();
        if ( utils != null )
          edge = utils.FindClosestEdge( camRay, camRayLength, 2.0f );
      }
      else {
      }

      return edge;
    }
  }
}
