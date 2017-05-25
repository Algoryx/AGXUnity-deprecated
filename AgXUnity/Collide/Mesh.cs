using System.Collections.Generic;
using AgXUnity.Utils;
using UnityEngine;

namespace AgXUnity.Collide
{
  /// <summary>
  /// Mesh object, convex or general trimesh, given source object
  /// render data.
  /// </summary>
  [AddComponentMenu( "AgXUnity/Shapes/Mesh" )]
  public sealed class Mesh : Shape
  {
    /// <summary>
    /// Deprecated source object instance - m_sourceObjects list is used now.
    /// </summary>
    [UnityEngine.Serialization.FormerlySerializedAs( "m_sourceObject" )]
    [SerializeField]
    private UnityEngine.Mesh m_legacySourceObject = null;

    /// <summary>
    /// Get or set source object (Unity Mesh).
    /// </summary>
    //[ShowInInspector]
    //public UnityEngine.Mesh SourceObject
    //{
    //  get { return m_sourceObject; }
    //  set
    //  {
    //    if ( value == m_sourceObject )
    //      return;

    //    // New source, destroy current debug rendering data.
    //    if ( m_sourceObject != null ) {
    //      Rendering.ShapeDebugRenderData data = GetComponent<Rendering.ShapeDebugRenderData>();
    //      if ( data != null )
    //        GameObject.DestroyImmediate( data.Node );
    //      m_sourceObject = null;
    //    }

    //    // New source.
    //    if ( m_sourceObject == null ) {
    //      m_sourceObject = value;

    //      // Instead of calling SizeUpdated we have to make sure
    //      // a complete new instance of the debug render object
    //      // is created (i.e., not only update scale if node exist).
    //      Rendering.DebugRenderManager.HandleMeshSource( this );
    //      Rendering.ShapeVisualMesh.HandleMeshSource( this );
    //    }
    //  }
    //}

    /// <summary>
    /// List of source mesh objects to include in the physical mesh.
    /// </summary>
    [SerializeField]
    private List<UnityEngine.Mesh> m_sourceObjects = new List<UnityEngine.Mesh>();

    /// <summary>
    /// Returns all source objects added to this shape.
    /// </summary>
    public UnityEngine.Mesh[] SourceObjects
    {
      get { return m_sourceObjects.ToArray(); }
    }

    /// <summary>
    /// Returns native mesh object if created.
    /// </summary>
    public agxCollide.Mesh Native { get { return m_shape as agxCollide.Mesh; } }

    /// <summary>
    /// Single source object assignment. All meshes that has been added before
    /// will be removed and <paramref name="mesh"/> added.
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    public bool SetSourceObject( UnityEngine.Mesh mesh )
    {
      m_sourceObjects.Clear();
      return AddSourceObject( mesh );
    }

    /// <summary>
    /// Add source mesh object to this shape.
    /// </summary>
    /// <param name="mesh">Source mesh.</param>
    /// <returns>True if added - otherwise false.</returns>
    public bool AddSourceObject( UnityEngine.Mesh mesh )
    {
      if ( mesh == null || m_sourceObjects.Contains( mesh ) )
        return false;

      m_sourceObjects.Add( mesh );

      return true;
    }

    /// <summary>
    /// Remove source mesh object from this shape.
    /// </summary>
    /// <param name="mesh">Source object to remove.</param>
    /// <returns>True if removed.</returns>
    public bool RemoveSourceObject( UnityEngine.Mesh mesh )
    {
      return m_sourceObjects.Remove( mesh );
    }

    /// <summary>
    /// Moves old single source to source list.
    /// </summary>
    /// <returns>True if changes were made.</returns>
    public bool PatchSingleSourceToSourceList()
    {
      if ( m_legacySourceObject == null )
        return false;

      m_sourceObjects.Add( m_legacySourceObject );
      m_legacySourceObject = null;

      return true;
    }

    /// <summary>
    /// </summary>
    public override Vector3 GetScale()
    {
      return Vector3.one;
    }

    /// <summary>
    /// Creates a native instance of the mesh and returns it. Performance warning.
    /// </summary>
    public override agxCollide.Shape CreateTemporaryNative()
    {
      return CreateNative();
    }

    /// <summary>
    /// Create the native mesh object given the current source mesh.
    /// </summary>
    protected override agxCollide.Shape CreateNative()
    {
      return Create( SourceObjects );
    }

    /// <summary>
    /// Override of initialize, only to delete any reference to a
    /// cached native object.
    /// </summary>
    protected override bool Initialize()
    {
      return base.Initialize();
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

      Matrix4x4 toWorld = transform.localToWorldMatrix;
      foreach ( Vector3 vertex in vertices )
        agxVertices.Add( transform.InverseTransformDirection( toWorld * vertex ).ToHandedVec3() );

      foreach ( var index in indices )
        agxIndices.Add( (uint)index );

      return isConvex ? new agxCollide.Convex( agxVertices, agxIndices, "Convex", (uint)agxCollide.Convex.TrimeshOptionsFlags.CLOCKWISE_ORIENTATION ) :
                        new agxCollide.Trimesh( agxVertices, agxIndices, "Trimesh", (uint)agxCollide.Trimesh.TrimeshOptionsFlags.CLOCKWISE_ORIENTATION );
    }

    private agxCollide.Mesh Create( UnityEngine.Mesh[] meshes )
    {
      var merger = MeshMerger.Merge( transform, meshes );
      return new agxCollide.Trimesh( merger.Vertices, merger.Indices, "Trimesh" );
    }
  }
}
