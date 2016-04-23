using UnityEditor;
using UnityEngine;
using AgXUnity.Collide;
using AgXUnity.Utils;

namespace AgXUnityEditor.Tools
{
  public class EdgeDetectionTool : Tool
  {
    private GameObject m_renderedEdge = null;

    private GameObject m_target = null;
    public GameObject Target
    {
      get { return m_target; }
      set { m_target = value; }
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( m_renderedEdge == null ) {
        m_renderedEdge = AgXUnity.Rendering.Spawner.CreateUnique( AgXUnity.Rendering.Spawner.Primitive.Cylinder, "AgXUnityEditor.EdgeDectionTool.Edge", HideFlags.HideAndDontSave, "Unlit/Color" );
        m_renderedEdge.SetActive( false );
      }

      m_renderedEdge.SetActive( false );

      if ( Target == null )
        return;

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
          edge = utils.FindClosestEdge( camRay, camRayLength, 10.0f );
      }
      else {
      }

      if ( edge != null ) {
        m_renderedEdge.SetActive( true );
        SetCylinderTransform( edge.Start, edge.End, 0.025f );
      }
    }

    private void SetCylinderTransform( Vector3 start, Vector3 end, float radius )
    {
      float r       = radius * Mathf.Max( HandleUtility.GetHandleSize( start ), HandleUtility.GetHandleSize( end ) );
      Vector3 dir   = end - start;
      float height  = dir.magnitude;
      dir          /= height;

      m_renderedEdge.transform.localScale = new Vector3( 2.0f * r, 0.5f * height, 2.0f * r );
      m_renderedEdge.transform.rotation   = Quaternion.FromToRotation( Vector3.up, dir );
      m_renderedEdge.transform.position   = 0.5f * ( start + end );
    }
  }
}
