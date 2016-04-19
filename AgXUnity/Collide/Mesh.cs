using AgXUnity.Utils;
using UnityEngine;

namespace AgXUnity.Collide
{
  /// <summary>
  /// Mesh object, convex or general trimesh, given source object
  /// render data.
  /// </summary>
  [AddComponentMenu( "AgXUnity/Shapes/Mesh" )]
  [GenerateCustomEditor]
  public sealed class Mesh : Shape
  {
    /// <summary>
    /// Cached instance when the source object is the same.
    /// </summary>
    private agxCollide.Shape m_cachedNative = null;

    /// <summary>
    /// Source object paired with property SourceObject.
    /// </summary>
    [SerializeField]
    private UnityEngine.Mesh m_sourceObject = null;
    /// <summary>
    /// Get or set source object (Unity Mesh).
    /// </summary>
    [ShowInInspector]
    public UnityEngine.Mesh SourceObject
    {
      get { return m_sourceObject; }
      set
      {
        if ( value == m_sourceObject )
          return;

        if ( m_sourceObject != value )
          m_cachedNative = null;

        // New source, destroy current debug rendering data.
        if ( m_sourceObject != null ) {
          Rendering.ShapeDebugRenderData data = GetComponent<Rendering.ShapeDebugRenderData>();
          if ( data != null )
            GameObject.DestroyImmediate( data.Node );
          m_sourceObject = null;
          SizeUpdated();
        }

        // New source.
        if ( m_sourceObject == null ) {
          m_sourceObject = value;
          // Create debug rendering data.
          SizeUpdated();
        }
      }
    }

    /// <summary>
    /// Returns native mesh object if created.
    /// </summary>
    public agxCollide.Mesh Native { get { return m_shape as agxCollide.Mesh; } }

    /// <summary>
    /// </summary>
    public override Vector3 GetScale()
    {
      return Vector3.one;
    }

    /// <summary>
    /// Creates new temporary native object or returns the current cached
    /// native object. We keep a cached version for performance reasons
    /// when this method is called a lot to determine the mass properties
    /// of RigidBody objects.
    /// </summary>
    public override agxCollide.Shape CreateTemporaryNative()
    {
      if ( m_cachedNative == null )
        m_cachedNative = CreateNative();
      return m_cachedNative;
    }

    /// <summary>
    /// Create the native mesh object given the current source mesh.
    /// </summary>
    protected override agxCollide.Shape CreateNative()
    {
      return Create( SourceObject );
    }

    /// <summary>
    /// Override of initialize, only to delete any reference to a
    /// cached native object.
    /// </summary>
    protected override bool Initialize()
    {
      m_cachedNative = null;
      return base.Initialize();
    }

    /// <summary>
    /// Extensible create method that translates Unity Mesh to
    /// vertices and triangles/indices.
    /// </summary>
    /// <param name="mesh">Unity Mesh object</param>
    /// <returns>Native mesh object if valid.</returns>
    private agxCollide.Mesh Create( UnityEngine.Mesh mesh )
    {
      if ( mesh == null )
        return null;
  
      return Create( mesh.vertices, mesh.triangles );
    }

    /// <summary>
    /// Creates native mesh object given vertices and indices.
    /// </summary>
    /// <remarks>
    /// Because of the left handedness of Unity the triangles are "clockwise".
    /// </remarks>
    /// <param name="vertices">Mesh vertices.</param>
    /// <param name="indices">Mesh indices/triangles.</param>
    /// <param name="isConvex">True if the mesh is convex, otherwise (default) false.</param>
    /// <returns>Native mesh object.</returns>
    private agxCollide.Mesh Create( Vector3[] vertices, int[] indices, bool isConvex = false )
    {
      agx.Vec3Vector agxVertices = new agx.Vec3Vector( vertices.Length );
      agx.UInt32Vector agxIndices = new agx.UInt32Vector( indices.Length );

      agx.AffineMatrix4x4 toLocalTransform = new agx.AffineMatrix4x4( transform.rotation.ToHandedQuat(), transform.position.ToHandedVec3() ).inverse();
      foreach ( Vector3 vertex in vertices ) {
        agx.Vec3 worldVertex = transform.TransformPoint( vertex ).ToHandedVec3();
        agxVertices.Add( toLocalTransform.preMult( worldVertex ) );
      }

      //Vector3 scale = transform.lossyScale;
      //foreach ( var vertex in vertices )
      //  agxVertices.Add( Vector3.Scale( vertex, scale ).ToHandedVec3() );

      foreach ( var index in indices )
        agxIndices.Add( (uint)index );

      return isConvex ? new agxCollide.Convex( agxVertices, agxIndices, "Convex", (uint)agxCollide.Convex.TrimeshOptionsFlags.CLOCKWISE_ORIENTATION ) :
                        new agxCollide.Trimesh( agxVertices, agxIndices, "Trimesh", (uint)agxCollide.Trimesh.TrimeshOptionsFlags.CLOCKWISE_ORIENTATION );
    }
  }
}
