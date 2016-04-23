using UnityEngine;

namespace AgXUnity.Utils
{
  public static class MeshUtils
  {
    /// <summary>
    /// Edge containing a start and an end point.
    /// </summary>
    public class Edge
    {
      /// <summary>
      /// Construct given start and end point.
      /// </summary>
      /// <param name="start">Start point.</param>
      /// <param name="end">End point.</param>
      public Edge( Vector3 start, Vector3 end, Vector3 normal ) { Start = start; End = end; Normal = normal; }

      /// <summary>
      /// Start point.
      /// </summary>
      public Vector3 Start;

      /// <summary>
      /// End point.
      /// </summary>
      public Vector3 End;

      /// <summary>
      /// Normal of this edge - if possible to calculate.
      /// </summary>
      public Vector3 Normal;

      /// <summary>
      /// Length of the edge.
      /// </summary>
      public float Length { get { return Vector3.Magnitude( End - Start ); } }

      /// <summary>
      /// Direction of the edge, start to end.
      /// </summary>
      public Vector3 Direction { get { return Vector3.Normalize( End - Start ); } }
    }

    /// <summary>
    /// Intersection test triangle vs ray with resulting point in the triangle and time t along the ray.
    /// </summary>
    /// <param name="ray">Ray in same coordinate system as the vertices.</param>
    /// <param name="rayLength">Length of the ray.</param>
    /// <param name="v1">First vertex of the triangle.</param>
    /// <param name="v2">Second vertex of the triangle.</param>
    /// <param name="v3">Third vertex of the triangle.</param>
    /// <param name="normal">Normal of the triangle.</param>
    /// <param name="result">If hit, resulting point inside the triangle.</param>
    /// <param name="t">Time along the ray (0, 1).</param>
    /// <returns>True if the ray intersects the triangle.</returns>
    public static bool IntersectRayTriangle( Ray ray, float rayLength, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal, ref Vector3 result, ref float t )
    {
      float epsilon    = 1.0E-6f;
      Vector3 lineP1   = ray.GetPoint( 0 );
      Vector3 lineP2   = ray.GetPoint( rayLength );
      Vector3 lineP1P2 = lineP2 - lineP1;

      float d = -Vector3.Dot( lineP1P2, normal );
      // Parallel or back face.
      if ( d < epsilon )
        return false;

      Vector3 triangleP1LineP1 = lineP1 - v1;
      t = Vector3.Dot( triangleP1LineP1, normal );

      if ( t < epsilon )
        return false;
      if ( t > d - epsilon )
        return false;

      t /= d;

      result = lineP1 + lineP1P2 * t;

      Vector3 a = v1 - result;
      Vector3 b = v2 - result;
      Vector3 c = v3 - result;

      float ab = Vector3.Dot( a, b );
      float ac = Vector3.Dot( a, c );
      float bc = Vector3.Dot( b, c );

      float cc = Vector3.Dot( c, c );
      float lagId0 = bc * ac - cc * ab;
      if ( lagId0 < -epsilon )
        return false;

      float bb = Vector3.Dot( b, b );
      float lagId1 = ab * bc - ac * bb;
      if ( lagId1 < -epsilon )
        return false;

      float aa = Vector3.Dot( a, a );
      float lagId2 = ac * ab - aa * bc;
      if ( lagId2 < -epsilon )
        return false;

      return true;
    }

    /// <summary>
    /// Resulting data of closest triangle search. CHECK Valid flag before using this result!
    /// </summary>
    public class FindTriangleResult
    {
      /// <summary>
      /// Distance from ray start to the intersection point of the triangle.
      /// </summary>
      public float Distance { get; set; }

      /// <summary>
      /// Index of the triangle in the mesh.triangles data.
      /// </summary>
      /// <remarks>
      /// Note that the local vertices doesn't include the scale.
      /// </remarks>
      /// <example>
      /// Vector3 vertex1 = mesh.sharedMesh.vertices[ mesh.sharedMesh.triangles[ TriangleIndex + 0 ] ];
      /// Vector3 vertex2 = mesh.sharedMesh.vertices[ mesh.sharedMesh.triangles[ TriangleIndex + 1 ] ];
      /// Vector3 vertex3 = mesh.sharedMesh.vertices[ mesh.sharedMesh.triangles[ TriangleIndex + 2 ] ];
      /// </example>
      public int TriangleIndex { get; set; }

      /// <summary>
      /// Index of the edge, closest to the point of intersection.
      /// </summary>
      public int ClosestEdgeIndex { get; set; }

      /// <summary>
      /// The mesh the test has been made on.
      /// </summary>
      public UnityEngine.Mesh Mesh { get; private set; }

      /// <summary>
      /// The parent game object the mesh transforms by. Invalid if null.
      /// </summary>
      public GameObject Parent { get; set; }

      /// <summary>
      /// Vertices of the closest triangle, given in world coordinate frame.
      /// </summary>
      public Vector3[] WorldVertices
      {
        get
        {
          if ( !Valid )
            return null;

          return new Vector3[]
                 {
                   Parent.transform.TransformPoint( Mesh.vertices[ Mesh.triangles[ TriangleIndex + 0 ] ] ),
                   Parent.transform.TransformPoint( Mesh.vertices[ Mesh.triangles[ TriangleIndex + 1 ] ] ),
                   Parent.transform.TransformPoint( Mesh.vertices[ Mesh.triangles[ TriangleIndex + 2 ] ] )
                 };
        }
      }

      /// <summary>
      /// Edges of the closest triangle, given in world coordinate frame.
      /// </summary>
      public Edge[] WorldEdges
      {
        get
        {
          Vector3[] worldVertices = WorldVertices;
          Vector3 worldNormal = WorldNormal;
          return new Edge[]
                 {
                   new Edge( worldVertices[ 0 ], worldVertices[ 1 ], worldNormal ),
                   new Edge( worldVertices[ 1 ], worldVertices[ 2 ], worldNormal ),
                   new Edge( worldVertices[ 2 ], worldVertices[ 0 ], worldNormal )
                 };
        }
      }

      /// <summary>
      /// Intersection point in the triangle, given in world coordinate frame.
      /// </summary>
      public Vector3 WorldIntersectionPoint { get; set; }

      /// <summary>
      /// Normal of the triangle given in world coordinate frame.
      /// </summary>
      public Vector3 WorldNormal { get; set; }

      /// <summary>
      /// True if this data is valid, e.g., the ray intersects a triangle.
      /// </summary>
      public bool Valid { get { return Mesh != null && TriangleIndex < Mesh.triangles.Length && Distance < float.PositiveInfinity; } }

      /// <summary>
      /// Construct given a mesh filter.
      /// </summary>
      /// <param name="mesh">Mesh filter.</param>
      public FindTriangleResult( UnityEngine.Mesh mesh, GameObject parent )
      {
        Invalidate( mesh, parent );
      }

      /// <summary>
      /// Invalidate current state.
      /// </summary>
      public void Invalidate()
      {
        Invalidate( null, null );
      }

      private void Invalidate( UnityEngine.Mesh mesh, GameObject parent )
      {
        Mesh                   = mesh;
        Parent                 = parent;
        Distance               = float.PositiveInfinity;
        TriangleIndex          = int.MaxValue;
        ClosestEdgeIndex       = int.MaxValue;
        WorldIntersectionPoint = Vector3.zero;
        WorldNormal            = Vector3.up;
      }
    }

    public struct TriangleTestResult
    {
      public int TriangleIndex;
      public float Time;
      public Vector3 PointInTriangle;
      public bool Hit;
    }

    /// <summary>
    /// Test single triangle given ray in same coordinate system as the vertices and normal.
    /// </summary>
    /// <param name="ray">Ray in same coordinate system as the vertices and normal.</param>
    /// <param name="rayLength">Length of the ray.</param>
    /// <param name="v1">First vertex.</param>
    /// <param name="v2">Second vertex.</param>
    /// <param name="v3">Third vertex.</param>
    /// <param name="normal">Normal of the triangle.</param>
    /// <returns>Local result of test.</returns>
    public static TriangleTestResult TestTriangle( Ray ray, float rayLength, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal )
    {
      TriangleTestResult result = new TriangleTestResult() { TriangleIndex = int.MaxValue, Time = float.MaxValue, PointInTriangle = Vector3.zero, Hit = false };
      result.Hit = IntersectRayTriangle( ray, rayLength, v1, v2, v3, normal, ref result.PointInTriangle, ref result.Time );
      return result;
    }

    /// <summary>
    /// Test all triangles in a mesh to find the first triangle that intersects the ray.
    /// </summary>
    /// <param name="worldRay">Ray given in world coordinates (e.g., HandleUtility.GUIPointToWorldRay( Event.current.mousePosition )).</param>
    /// <param name="rayLength">Length of the ray.</param>
    /// <param name="mesh">The mesh.</param>
    /// <param name="parent">Parent game object that transforms the mesh.</param>
    /// <returns>Result of the test.</returns>
    public static FindTriangleResult TestAllTriangles( Ray worldRay, float rayLength, UnityEngine.Mesh mesh, GameObject parent )
    {
      FindTriangleResult result = new FindTriangleResult( mesh, parent );
      if ( mesh == null || parent == null )
        return result;

      // Mesh bounds are in local coordinates - transform ray to local.
      Ray localRay = new Ray( parent.transform.InverseTransformPoint( worldRay.origin ), parent.transform.InverseTransformVector( worldRay.direction ).normalized );
      if ( !mesh.bounds.IntersectRay( localRay ) )
        return result;

      int[] triangles    = mesh.triangles;
      Vector3[] vertices = mesh.vertices;
      for ( int i = 0; i < triangles.Length; i += 3 ) {
        Vector3 v1     = vertices[ triangles[ i + 0 ] ];
        Vector3 v2     = vertices[ triangles[ i + 1 ] ];
        Vector3 v3     = vertices[ triangles[ i + 2 ] ];
        Vector3 normal = Vector3.Cross( v2 - v1, v3 - v1 ).normalized;
        TriangleTestResult test = TestTriangle( localRay, rayLength, v1, v2, v3, normal );
        if ( test.Hit && test.Time < result.Distance ) {
          result.TriangleIndex          = i;
          result.Distance               = test.Time;
          result.WorldIntersectionPoint = parent.transform.TransformPoint( test.PointInTriangle );
          result.WorldNormal            = normal;
        }
      }

      if ( result.TriangleIndex >= triangles.Length )
        return result;

      result.Distance = Vector3.Distance( worldRay.GetPoint( 0 ), result.WorldIntersectionPoint );

      return result;
    }

    /// <summary>
    /// Finds closest triangle to ray start. Tests will be made against <paramref name="cachedResult"/> if
    /// given, and if hit, that result will be updated and used.
    /// </summary>
    /// <param name="mesh">The mesh.</param>
    /// <param name="parent">Parent game object that transforms the mesh.</param>
    /// <param name="worldRay">Ray in world coordinate frame.</param>
    /// <param name="rayLength">Length of the ray.</param>
    /// <param name="cachedResult">Already calculated result for this mesh to test against again.</param>
    /// <returns>Data with result, result.Valid == true if the ray intersects a triangle.</returns>
    public static FindTriangleResult FindClosestTriangle( UnityEngine.Mesh mesh, GameObject parent, Ray worldRay, float rayLength = 500.0f, FindTriangleResult cachedResult = null )
    {
      FindTriangleResult result = null;
      bool testCached = cachedResult != null && cachedResult.Valid && cachedResult.Mesh == mesh;
      if ( testCached ) {
        Vector3[] vertices = cachedResult.WorldVertices;
        TriangleTestResult test = TestTriangle( worldRay, rayLength, vertices[ 0 ], vertices[ 1 ], vertices[ 2 ], cachedResult.WorldNormal );
        if ( test.Hit ) {
          cachedResult.WorldIntersectionPoint = test.PointInTriangle;
          result = cachedResult;
          result.Distance = Vector3.Distance( worldRay.GetPoint( 0 ), test.PointInTriangle );
        }
      }

      if ( result == null )
        result = TestAllTriangles( worldRay, rayLength, mesh, parent );

      if ( !result.Valid )
        return result;

      Edge[] edges = result.WorldEdges;
      float closestEdgeDistance = float.PositiveInfinity;
      for ( int i = 0; i < edges.Length; ++i ) {
        float dist = ShapeUtils.ShortestDistancePointLine( result.WorldIntersectionPoint, edges[ i ].Start, edges[ i ].End );
        if ( dist < closestEdgeDistance ) {
          closestEdgeDistance = dist;
          result.ClosestEdgeIndex = i;
        }
      }

      return result;
    }

    /// <summary>
    /// Finds closest triangle to ray start in game object with one or many MeshFilters. Tests will be made against <paramref name="cachedResult"/> if
    /// given, and if hit, that result will be updated and used.
    /// </summary>
    /// <param name="parentGameObject">Object with render data (MeshFilters).</param>
    /// <param name="worldRay">Ray given in world coordinate system.</param>
    /// <param name="rayLength">Length of the ray.</param>
    /// <param name="cachedResult">Already calculated result for a mesh in this game object, to test against again.</param>
    /// <returns>Data with result, result.Valid == true if the ray intersects a triangle.</returns>
    public static FindTriangleResult FindClosestTriangle( GameObject parentGameObject, UnityEngine.Ray worldRay, float rayLength = 500.0f, FindTriangleResult cachedResult = null )
    {
      if ( parentGameObject == null )
        return new FindTriangleResult( null, null );

      UnityEngine.MeshFilter[] meshFilters = parentGameObject.GetComponentsInChildren<UnityEngine.MeshFilter>();
      if ( meshFilters.Length == 0 )
        return new FindTriangleResult( null, null );

      FindTriangleResult[] results = new FindTriangleResult[ meshFilters.Length ];
      for ( int i = 0; i < meshFilters.Length; ++i )
        results[ i ] = FindClosestTriangle( meshFilters[ i ].sharedMesh, meshFilters[ i ].gameObject, worldRay, rayLength, cachedResult );

      FindTriangleResult best = results[ 0 ];
      for ( int i = 1; i < results.Length; ++i )
        if ( results[ i ].Distance < best.Distance )
          best = results[ i ];

      return best;
    }
  }
}
