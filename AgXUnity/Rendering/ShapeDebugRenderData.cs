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

    /// <summary>
    /// True if the shape type is mesh.
    /// </summary>
    [HideInInspector]
    public bool IsMesh { get { return PrefabName.Contains( "Mesh" ) || PrefabName.Contains( "HeightField" ); } }

    /// <returns>The Collide.Shape component.</returns>
    public Shape GetShape() { return GetComponent<Shape>(); }

    /// <summary>
    /// Creates debug rendering node (if not already created) and
    /// synchronizes the transform.
    /// </summary>
    public override void Synchronize()
    {
      try {
        TryInitialize();

        Shape shape               = GetShape();
        Node.transform.localScale = shape.GetScale();
        Node.transform.position   = shape.transform.position;
        Node.transform.rotation   = shape.transform.rotation;
      }
      catch ( System.Exception ) {
      }
    }

    /// <summary>
    /// If no "Node" instance, this method tries to create one
    /// given the Collide.Shape component in this game object.
    /// </summary>
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
    }

    /// <summary>
    /// Initializes and returns a game object if the Collide.Shape type
    /// is of type mesh. Fails the the shape type is different from mesh.
    /// </summary>
    /// <returns>Game object with mesh renderer.</returns>
    private GameObject InitializeMesh()
    {
      Shape shape = GetShape();
      if ( shape == null || ( shape as Collide.Mesh == null && shape as Collide.HeightField == null ) )
        throw new Exception( "Unexpected behavior where ShapeDebugRenderData is not component of AgXUnity.Shape." );

      if ( shape as Collide.HeightField != null )
        return InitializeHeightField( shape as Collide.HeightField );

      return InitializeMeshGivenSourceObject( shape as Collide.Mesh );
    }

    /// <summary>
    /// Initializes debug render object given the source object of the
    /// Collide.Mesh component.
    /// </summary>
    private GameObject InitializeMeshGivenSourceObject( Collide.Mesh mesh )
    {
      if ( mesh == null )
        throw new ArgumentNullException( "mesh" );

      if ( mesh.SourceObject == null )
        throw new AgXUnity.Exception( "Mesh has no source." );

      GameObject meshData = new GameObject( "MeshData" );
      MeshRenderer renderer = meshData.AddComponent<MeshRenderer>();
      MeshFilter filter = meshData.AddComponent<MeshFilter>();

      renderer.sharedMaterial = Resources.Load<UnityEngine.Material>( "Materials/DebugRendererMaterial" );
      filter.sharedMesh = mesh.SourceObject;

      return meshData;
    }

    /// <summary>
    /// Debug rendering of HeightField is currently not supported.
    /// </summary>
    private GameObject InitializeHeightField( HeightField hf )
    {
      return new GameObject( "HeightFieldData" );
    }
  }
}
