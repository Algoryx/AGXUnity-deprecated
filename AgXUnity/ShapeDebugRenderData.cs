using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using AgXUnity.Collide;
using AgXUnity.Utils;

namespace AgXUnity
{
  [GenerateCustomEditor]
  public class ShapeDebugRenderData : DebugRenderData
  {
    public override string GetTypeName()
    {
      return GetShape().GetType().Name;
    }

    [HideInInspector]
    public bool IsMesh { get { return PrefabName.Contains( "Mesh" ) || PrefabName.Contains( "HeightField" ); } }

    public Shape GetShape() { return GetComponent<Shape>(); }

    public override void Synchronize()
    {
      try {
        TryInitialize();

        Shape shape = GetShape();
        Node.transform.localScale = shape.GetScale();
      }
      catch ( System.Exception ) {
      }
    }

    private void TryInitialize()
    {
      if ( Node != null )
        return;

      if ( IsMesh )
        Node = InitializeMesh();
      else {
        Node = PrefabLoader.Instantiate( PrefabName );
        Node.transform.localScale = GetShape().GetScale();
      }

      Node.hideFlags = HideFlags.HideAndDontSave;
      foreach ( Transform child in Node.transform )
        child.gameObject.hideFlags = HideFlags.HideAndDontSave;

      gameObject.AddChild( Node );
    }

    private GameObject InitializeMesh()
    {
      Shape shape = GetShape();
      if ( shape == null || ( shape as Collide.Mesh == null && shape as Collide.HeightField == null ) )
        throw new Exception( "Unexpected behavior where ShapeDebugRenderData is not component of AgXUnity.Shape." );

      if ( shape as Collide.HeightField != null )
        return InitializeHeightField( shape as Collide.HeightField );

      bool useSourceObjectMesh = true;
      if ( useSourceObjectMesh )
        return InitializeMeshGivenSourceObject( shape as Collide.Mesh );
      else
        return InitializeMeshGivenNative( shape as Collide.Mesh );
    }

    private GameObject InitializeMeshGivenSourceObject( Collide.Mesh mesh )
    {
      if ( mesh == null )
        throw new ArgumentNullException( "mesh" );

      if ( mesh.SourceObject == null )
        throw new AgXUnity.Exception( "Mesh has no source." );

      GameObject meshData = new GameObject( "MeshData" );
      MeshRenderer renderer = meshData.AddComponent<MeshRenderer>();
      MeshFilter filter = meshData.AddComponent<MeshFilter>();

      renderer.sharedMaterial = Resources.Load<UnityEngine.Material>( "Debug/DebugRendererMaterial" );
      filter.sharedMesh = mesh.SourceObject;

      return meshData;
    }

    private GameObject InitializeMeshGivenNative( Collide.Mesh mesh )
    {
      if ( mesh == null )
        throw new ArgumentNullException( "mesh" );

      agxCollide.Mesh nativeMesh = mesh.CreateTemporaryNative() as agxCollide.Mesh;
      if ( nativeMesh == null || nativeMesh.getNumVertices() == 0 )
        throw new Exception( "Mesh not initialized." );

      Vector3[] vertices = new Vector3[ nativeMesh.getNumVertices() ];
      for ( uint i = 0; i < nativeMesh.getNumVertices(); ++i )
        vertices[ i ] = nativeMesh.getVertex( i ).AsVector3();

      int[] triangles = new int[ 3 * nativeMesh.getNumTriangles() ];
      for ( uint i = 0; i < nativeMesh.getNumTriangles(); ++i ) {
        triangles[ 3 * i + 0 ] = Convert.ToInt32( nativeMesh.getGlobalVertexIndex( i, 0 ) );
        triangles[ 3 * i + 1 ] = Convert.ToInt32( nativeMesh.getGlobalVertexIndex( i, 1 ) );
        triangles[ 3 * i + 2 ] = Convert.ToInt32( nativeMesh.getGlobalVertexIndex( i, 2 ) );
      }

      Vector2[] uv = new Vector2[ vertices.Length ];
      for ( int i = 0; i < vertices.Length; ++i )
        uv[ i ] = new Vector2( vertices[ i ].x, vertices[ i ].z );

      UnityEngine.Mesh unityMesh = new UnityEngine.Mesh();
      unityMesh.vertices = vertices;
      unityMesh.triangles = triangles;
      unityMesh.uv = uv;
      unityMesh.RecalculateNormals();

      GameObject meshData = new GameObject( "MeshData", typeof( MeshFilter ), typeof( MeshRenderer ) );
      meshData.GetComponent<MeshFilter>().mesh = unityMesh;
      Material material = Resources.Load<UnityEngine.Material>( "Debug/DebugRendererMaterial" );
      meshData.GetComponent<MeshRenderer>().material = material;

      return meshData;
    }

    public GameObject InitializeHeightField( HeightField hf )
    {
      return new GameObject( "HeightFieldData" );
    }
  }
}
