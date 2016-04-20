﻿using System;
using UnityEngine;
using AgXUnity.Collide;
using AgXUnity.Utils;

namespace AgXUnity.Rendering
{
  /// <summary>
  /// Debug rendering component which is added to all game objects
  /// containing Collide.Shape components. DebugRenderManager manages
  /// these objects.
  /// </summary>
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
  public class ShapeDebugRenderData : DebugRenderData
  {
    /// <summary>
    /// Typename is shape type - prefabs in Resources folder has been
    /// named to fit these names.
    /// </summary>
    /// <returns></returns>
    public override string GetTypeName()
    {
      return GetShape().GetType().Name;
    }

    /// <returns>The Collide.Shape component.</returns>
    public Shape GetShape() { return GetComponent<Shape>(); }

    /// <summary>
    /// Lossy scale (of the shape) stored to know when to rescale the
    /// debug rendered mesh of Collide.Mesh objects.
    /// </summary>
    [SerializeField]
    private Vector3 m_storedLossyScale = Vector3.one;

    /// <summary>
    /// Creates debug rendering node if it doesn't already exist and
    /// synchronizes the rendered object transform to be the same as the shape.
    /// </summary>
    /// <param name="manager"></param>
    public override void Synchronize( DebugRenderManager manager )
    {
      try {
        Shape shape      = GetShape();
        bool nodeCreated = TryInitialize( shape );

        if ( Node == null )
          return;

        // Node created - set properties and extra components.
        if ( nodeCreated ) {
          Node.hideFlags = HideFlags.DontSave;
          Node.GetOrCreateComponent<OnSelectionProxy>().Target = shape.gameObject;
          foreach ( Transform child in Node.transform )
            child.gameObject.GetOrCreateComponent<OnSelectionProxy>().Target = shape.gameObject;
        }

        // Forcing the debug render node to be parent to the static DebugRenderManger.
        if ( Node.transform.parent != manager.gameObject.transform )
          manager.gameObject.AddChild( Node );

        Node.transform.position = shape.transform.position;
        Node.transform.rotation = shape.transform.rotation;

        SynchronizeScale( shape );
      }
      catch ( System.Exception ) {
      }
    }

    /// <summary>
    /// Synchronize the scale/size of the debug render object to match the shape size.
    /// Scaling is ignore if the node hasn't been created (i.e., this method doesn't
    /// create the render node).
    /// </summary>
    /// <param name="shape">Shape this component belongs to.</param>
    public void SynchronizeScale( Collide.Shape shape )
    {
      if ( Node == null )
        return;

      Node.transform.localScale = shape.GetScale();

      if ( shape is Collide.Mesh ) {
        if ( m_storedLossyScale != transform.lossyScale ) {
          RescaleRenderedMesh( shape as Collide.Mesh, Node.GetComponent<MeshFilter>() );
          m_storedLossyScale = transform.lossyScale;
        }
      }
      else if ( shape is Collide.Capsule ) {
        if ( Node.transform.childCount != 3 )
          throw new Exception( "Capsule debug rendering node doesn't contain three children." );

        Collide.Capsule capsule   = shape as Capsule;
        Transform cylinder        = Node.transform.GetChild( 0 );
        Transform sphereUpper     = Node.transform.GetChild( 1 );
        Transform sphereLower     = Node.transform.GetChild( 2 );

        cylinder.localScale       = new Vector3( 2.0f * capsule.Radius, 0.5f * capsule.Height, 2.0f * capsule.Radius );

        sphereUpper.localScale    = 2.0f * capsule.Radius * Vector3.one;
        sphereUpper.localPosition = 0.5f * capsule.Height * Vector3.up;

        sphereLower.localScale    = 2.0f * capsule.Radius * Vector3.one;
        sphereLower.localPosition = 0.5f * capsule.Height * Vector3.down;
      }
    }

    /// <summary>
    /// If no "Node" instance, this method tries to create one
    /// given the Collide.Shape component in this game object.
    /// </summary>
    /// <returns>True if the node was created - otherwise false.</returns>
    private bool TryInitialize( Collide.Shape shape )
    {
      if ( Node != null )
        return false;

      Collide.Mesh mesh               = shape as Collide.Mesh;
      Collide.HeightField heightField = shape as Collide.HeightField;
      if ( mesh != null )
        Node = InitializeMesh( mesh );
      else if ( heightField != null )
        Node = InitializeHeightField( heightField );
      else {
        Node = PrefabLoader.Instantiate( PrefabName );
        Node.transform.localScale = GetShape().GetScale();
      }

      return Node != null;
    }

    /// <summary>
    /// Initializes and returns a game object if the Collide.Shape type
    /// is of type mesh. Fails the the shape type is different from mesh.
    /// </summary>
    /// <returns>Game object with mesh renderer.</returns>
    private GameObject InitializeMesh( Collide.Mesh mesh )
    {
      return InitializeMeshGivenSourceObject( mesh );
    }

    /// <summary>
    /// Initializes debug render object given the source object of the
    /// Collide.Mesh component.
    /// </summary>
    private GameObject InitializeMeshGivenSourceObject( Collide.Mesh mesh )
    {
      if ( mesh.SourceObject == null )
        throw new AgXUnity.Exception( "Mesh has no source." );

      GameObject meshData = new GameObject( "MeshData" );
      MeshRenderer renderer = meshData.AddComponent<MeshRenderer>();
      MeshFilter filter = meshData.AddComponent<MeshFilter>();

      filter.sharedMesh = new UnityEngine.Mesh();

      RescaleRenderedMesh( mesh, filter );

      renderer.sharedMaterial = Resources.Load<UnityEngine.Material>( "Materials/DebugRendererMaterial" );
      m_storedLossyScale = mesh.transform.lossyScale;

      return meshData;
    }

    /// <summary>
    /// Debug rendering of HeightField is currently not supported.
    /// </summary>
    private GameObject InitializeHeightField( HeightField hf )
    {
      return new GameObject( "HeightFieldData" );
    }

    private void RescaleRenderedMesh( Collide.Mesh mesh, MeshFilter filter )
    {
      UnityEngine.Mesh source = mesh.SourceObject;
      if ( source == null )
        throw new AgXUnity.Exception( "Source object is null during rescale." );

      Vector3[] vertices = filter.sharedMesh.vertices;
      if ( vertices == null || vertices.Length == 0 )
        vertices = new Vector3[ source.vertexCount ];

      int[] triangles = filter.sharedMesh.triangles;
      if ( triangles == null || triangles.Length == 0 )
        triangles = (int[])source.triangles.Clone();

      if ( vertices.Length != source.vertexCount )
        throw new AgXUnity.Exception( "Shape debug render mesh mismatch." );

      Matrix4x4 scaledToWorld  = mesh.transform.localToWorldMatrix;
      Vector3[] sourceVertices = mesh.SourceObject.vertices;

      // Transforms each vertex from local to world given scales, then
      // transfroms each vertex back to local again - unscaled.
      for ( int i = 0; i < vertices.Length; ++i ) {
        Vector3 worldVertex = scaledToWorld * sourceVertices[ i ];
        vertices[ i ]       = mesh.transform.InverseTransformDirection( worldVertex );
      }

      filter.sharedMesh.vertices  = vertices;
      filter.sharedMesh.triangles = triangles;

      filter.sharedMesh.RecalculateBounds();
      filter.sharedMesh.RecalculateNormals();
    }
  }
}
