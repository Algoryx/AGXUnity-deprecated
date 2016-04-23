using UnityEngine;
using AgXUnity.Collide;

namespace AgXUnity.Utils
{
  public class BoxShapeUtils : ShapeUtils
  {
    public override Vector3 GetLocalFace( Direction dir )
    {
      Box box = GetShape<Box>();
      return Vector3.Scale( box.HalfExtents, GetLocalFaceDirection( dir ) );
    }

    public override bool IsHalfSize( Direction direction )
    {
      return true;
    }

    public override void UpdateSize( Vector3 localChange, Direction dir )
    {
      Box box = GetShape<Box>();
      box.HalfExtents = box.HalfExtents + Vector3.Scale( GetLocalFaceDirection( dir ), localChange );
    }
  }

  public class CapsuleShapeUtils : ShapeUtils
  {
    public override Vector3 GetLocalFace( Direction dir )
    {
      Capsule capsule = GetShape<Capsule>();
      if ( ToPrincipal( dir ) == PrincipalAxis.Y )
        return ( capsule.Radius + 0.5f * capsule.Height ) * GetLocalFaceDirection( dir );
      else
        return capsule.Radius * GetLocalFaceDirection( dir );
    }

    public override bool IsHalfSize( Direction direction )
    {
      return ToPrincipal( direction ) != PrincipalAxis.Y;
    }

    public override void UpdateSize( Vector3 localChange, Direction dir )
    {
      Capsule capsule = GetShape<Capsule>();
      if ( ToPrincipal( dir ) == PrincipalAxis.Y )
        capsule.Height = capsule.Height + Vector3.Dot( GetLocalFaceDirection( dir ), localChange );
      else
        capsule.Radius = capsule.Radius + Vector3.Dot( GetLocalFaceDirection( dir ), localChange );
    }
  }

  public class CylinderShapeUtils : ShapeUtils
  {
    public override Vector3 GetLocalFace( Direction dir )
    {
      Cylinder capsule = GetShape<Cylinder>();
      if ( ToPrincipal( dir ) == PrincipalAxis.Y )
        return 0.5f * capsule.Height * GetLocalFaceDirection( dir );
      else
        return capsule.Radius * GetLocalFaceDirection( dir );
    }

    public override bool IsHalfSize( Direction direction )
    {
      return ToPrincipal( direction ) != PrincipalAxis.Y;
    }

    public override void UpdateSize( Vector3 localChange, Direction dir )
    {
      Cylinder cylinder = GetShape<Cylinder>();
      if ( ToPrincipal( dir ) == PrincipalAxis.Y )
        cylinder.Height = cylinder.Height + Vector3.Dot( GetLocalFaceDirection( dir ), localChange );
      else
        cylinder.Radius = cylinder.Radius + Vector3.Dot( GetLocalFaceDirection( dir ), localChange );
    }
  }

  public class SphereShapeUtils : ShapeUtils
  {
    public override Vector3 GetLocalFace( Direction dir )
    {
      Sphere sphere = GetShape<Sphere>();
      return sphere.Radius * GetLocalFaceDirection( dir );
    }

    public override bool IsHalfSize( Direction direction )
    {
      return true;
    }

    public override void UpdateSize( Vector3 localChange, Direction dir )
    {
      Sphere sphere = GetShape<Sphere>();
      sphere.Radius = sphere.Radius + Vector3.Dot( GetLocalFaceDirection( dir ), localChange );
    }
  }

  public class RaycastResult
  {
    public Vector3 Position { get; set; }

    public Vector3 Normal { get; set; }

    public MeshUtils.Edge Edge { get; set; }
  }

  public abstract class ShapeUtils
  {
    /// <summary>
    /// Calculates shortest distance between a point and a line segment.
    /// </summary>
    /// <param name="point">Point.</param>
    /// <param name="segmentStart">Segment start.</param>
    /// <param name="segmentEnd">Segment end.</param>
    /// <returns>Shortest distance between the given point and the line segment.</returns>
    public static float ShortestDistancePointLine( Vector3 point, Vector3 segmentStart, Vector3 segmentEnd )
    {
      Vector3 segmentDir = segmentEnd - segmentStart;
      float divisor = segmentDir.sqrMagnitude;
      if ( divisor < 1.0E-6f )
        return Vector3.Distance( point, segmentStart );

      float t = Mathf.Clamp01( Vector3.Dot( ( point - segmentStart ), segmentDir ) / divisor );
      Vector3 segmentPoint = ( 1.0f - t ) * segmentStart + t * segmentEnd;

      return Vector3.Distance( point, segmentPoint );
    }

    /// <summary>
    /// Finds shortest distance between two line segments.
    /// </summary>
    /// <param name="segment1Begin">Begin point, first segment.</param>
    /// <param name="segment1End">End point, first segment.</param>
    /// <param name="segment2Begin">Begin point, second segment.</param>
    /// <param name="segment2End">End point, second segment.</param>
    /// <returns>Shortest distance between the two line segments.</returns>
    public static float ShortestDistanceSegmentSegment( Vector3 segment1Begin, Vector3 segment1End, Vector3 segment2Begin, Vector3 segment2End )
    {
      float eps       = float.Epsilon;
      Vector3 d1      = segment1End - segment1Begin;
      Vector3 d2      = segment2End - segment2Begin;
      Vector3 r       = segment1Begin - segment2Begin;

      float d1Length2 = Vector3.Dot( d1, d1 );
      float d2Length2 = Vector3.Dot( d2, d2 );
      float d2r       = Vector3.Dot( r, d2 );

      float t1        = 0.0f;
      float t2        = 0.0f;
      float pt1       = 0.0f;
      float pt2       = 0.0f;
      bool isParallel = false;

      if ( d1Length2 <= eps && d2Length2 <= eps )
        return Vector3.Distance( segment1Begin, segment2Begin );

      if ( d1Length2 <= eps ) {
        t1 = 0.0f;
        t2 = Mathf.Clamp01( d2r / d2Length2 );
      }
      else {
        float d1r = Vector3.Dot( d1, r );
        if ( d2Length2 <= eps ) {
          t2 = 0.0f;
          t1 = Mathf.Clamp01( -d1r / d1Length2 );
        }
        else {
          float d1d2 = Vector3.Dot( d1, d2 );
          float denom = d1Length2 * d2Length2 - d1d2 * d1d2;
          int numPairsToFind = 1;
          if ( denom <= eps ) {
            isParallel = true;
            numPairsToFind = 2;
            t1 = 0.0f;
          }
          else
            t1 = Mathf.Clamp01( ( d2r * d1d2 - d1r * d2Length2 ) / denom );

          while ( numPairsToFind > 0 ) {
            t2 = ( d1d2 * t1 + d2r ) / d2Length2;

            if ( t2 < 0.0f ) {
              t2 = 0.0f;
              t1 = Mathf.Clamp01( -d1r / d1Length2 );
            }
            else if ( t2 > 1.0f ) {
              t2 = 1.0f;
              t1 = Mathf.Clamp01( ( d1d2 - d1r ) / d1Length2 );
            }

            if ( numPairsToFind == 2 ) {
              pt1 = t1;
              pt2 = t2;
              t1 = 1.0f;
            }

            --numPairsToFind;
          }

          if ( isParallel ) {
            t1 = pt1;
            t2 = pt2;
          }
        }
      }

      return Vector3.Distance( segment1Begin + t1 * d1, segment2Begin + t2 * d2 );
    }

    public static MeshUtils.Edge FindClosestEdge( Ray ray, float rayLength, MeshUtils.Edge[] edges )
    {
      int bestEdge = edges.Length;
      float bestDistance = float.MaxValue;
      for ( int i = 0; i < edges.Length; ++i ) {
        var edge = edges[ i ];
        if ( edge == null )
          continue;

        float distance = ShortestDistanceSegmentSegment( ray.GetPoint( 0 ), ray.GetPoint( rayLength ), edge.Start, edge.End );
        if ( distance < bestDistance ) {
          bestDistance = distance;
          bestEdge = i;
        }
      }

      return bestEdge < edges.Length ? edges[ bestEdge ] : null;
    }

    public static ShapeUtils Create( Shape shape )
    {
      if ( shape is Box )
        return new BoxShapeUtils() { m_shape = shape };
      else if ( shape is Capsule )
        return new CapsuleShapeUtils() { m_shape = shape };
      else if ( shape is Cylinder )
        return new CylinderShapeUtils() { m_shape = shape };
      else if ( shape is Sphere )
        return new SphereShapeUtils() { m_shape = shape };

      return null;
    }

    public enum PrincipalAxis
    {
      X,
      Y,
      Z
    }

    public enum Direction
    {
      Positive_X,
      Negative_X,
      Positive_Y,
      Negative_Y,
      Positive_Z,
      Negative_Z
    }

    public abstract Vector3 GetLocalFace( Direction direction );

    public abstract bool IsHalfSize( Direction direction );

    public abstract void UpdateSize( Vector3 localChange, Direction dir );

    public Vector3 GetLocalFaceDirection( Direction direction )
    {
      return m_unitFaces[ System.Convert.ToInt32( direction ) ];
    }

    public PrincipalAxis ToPrincipal( Direction dir )
    {
      return (PrincipalAxis)( System.Convert.ToInt32( dir ) / 2 );
    }

    public float GetSign( Direction dir )
    {
      return 1.0f - 2.0f * ( System.Convert.ToInt32( dir ) % 2 );
    }

    public Vector3 GetWorldFace( Direction direction )
    {
      return m_shape.transform.position + m_shape.transform.TransformDirection( GetLocalFace( direction ) );
    }

    public Vector3 GetWorldFaceDirection( Direction direction )
    {
      return m_shape.transform.TransformDirection( GetLocalFaceDirection( direction ) );
    }

    private static GameObject m_raycastGameObject = null;

    public RaycastResult Raycast( Ray ray, float rayLength = 500.0f )
    {
      string name = Rendering.DebugRenderData.GetPrefabName( m_shape.GetType().Name );
      GameObject tmp = PrefabLoader.Instantiate( name );
      if ( tmp == null )
        return null;

      tmp.hideFlags          = HideFlags.HideAndDontSave;
      tmp.transform.position = m_shape.transform.position;
      tmp.transform.rotation = m_shape.transform.rotation;

      MeshUtils.FindTriangleResult tmpResult = MeshUtils.FindClosestTriangle( tmp, ray, rayLength );
      RaycastResult result = null;
      if ( tmpResult.Valid )
        result = new RaycastResult() { Position = tmpResult.WorldIntersectionPoint, Normal = tmpResult.WorldNormal, Edge = FindClosestEdge( ray, rayLength, tmpResult.WorldEdges ) };

      GameObject.DestroyImmediate( tmp );

      return result;
    }

    /// <summary>
    /// Finds closest edge given an edge function which, e.g., tests the edge
    /// against a given ray.
    /// </summary>
    /// <param name="edgeFunc">
    /// Function taking and edge and returns the shortest distance
    /// from a given, arbitrary segment, to that edge.
    /// </param>
    /// <param name="principalEdgeExtension"></param>
    /// <returns></returns>
    public MeshUtils.Edge FindClosestEdge( Ray ray, float rayLength, float principalEdgeExtension )
    {
      // 3 principal edges and 1 closest given ray cast.
      MeshUtils.Edge[] edges = new MeshUtils.Edge[ 4 ];

      edges[ 0 ] = new MeshUtils.Edge( GetLocalFace( Direction.Negative_X ), GetLocalFace( Direction.Positive_X ), GetLocalFaceDirection( Direction.Positive_Y ) );
      edges[ 1 ] = new MeshUtils.Edge( GetLocalFace( Direction.Negative_Y ), GetLocalFace( Direction.Positive_Y ), GetLocalFaceDirection( Direction.Positive_Z ) );
      edges[ 2 ] = new MeshUtils.Edge( GetLocalFace( Direction.Negative_Z ), GetLocalFace( Direction.Positive_Z ), GetLocalFaceDirection( Direction.Positive_X ) );

      for ( int i = 0; i < 3; ++i ) {
        edges[ i ].Start -= principalEdgeExtension * edges[ i ].Direction;
        edges[ i ].End   += principalEdgeExtension * edges[ i ].Direction;
        edges[ i ].Start  = m_shape.transform.position + m_shape.transform.TransformDirection( edges[ i ].Start );
        edges[ i ].End    = m_shape.transform.position + m_shape.transform.TransformDirection( edges[ i ].End );
        edges[ i ].Normal = m_shape.transform.TransformDirection( edges[ i ].Normal );
      }

      var raycastResult = Raycast( ray, rayLength );
      edges[ 3 ] = raycastResult != null ? raycastResult.Edge : null;

      return FindClosestEdge( ray, rayLength, edges );
    }

    public T GetShape<T>() where T : Shape
    {
      return m_shape as T;
    }

    private static Vector3[] m_unitFaces = new Vector3[]{
                                                          new Vector3(  1,  0,  0 ),
                                                          new Vector3( -1,  0,  0 ),
                                                          new Vector3(  0,  1,  0 ),
                                                          new Vector3(  0, -1,  0 ),
                                                          new Vector3(  0,  0,  1 ),
                                                          new Vector3(  0,  0, -1 )
                                                        };

    private Shape m_shape = null;
  }
}
