using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;

namespace AgXUnityEditor
{
  public class AssetPostprocessorHandler : AssetPostprocessor
  {
    private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
    {
      foreach ( var import in importedAssets ) {
        FileInfo info = new FileInfo( import );
        if ( info.Extension == ".agx" || info.Extension == ".aagx" )
          ReadAGXFile( info );
      }
    }

    private void OnPreprocessModel()
    {
    }

    private static void ReadGeometry( agxCollide.Geometry nativeGeometry,
                                      GameObject parent,
                                      FileInfo prefabFile,
                                      string prefabPath,
                                      string prefabName )
    {
      foreach ( var nativeShape in nativeGeometry.getShapes() ) {
        var type = (agxCollide.Shape.Type)nativeShape.getType();
        var shapeGameObject = new GameObject( nativeGeometry.getName() );
        // getTransform(): local to global
        var nativeShapeToWorld = nativeShape.getTransform();
        parent.AddChild( shapeGameObject );
        shapeGameObject.transform.position = nativeShapeToWorld.getTranslate().ToHandedVector3();
        shapeGameObject.transform.rotation = nativeShapeToWorld.getRotate().ToHandedQuaternion();

        if ( type == agxCollide.Shape.Type.BOX ) {
          Debug.Log( "Shape.Type.BOX" );
          AgXUnity.Collide.Box box = shapeGameObject.AddComponent<AgXUnity.Collide.Box>();
          box.HalfExtents = nativeShape.get().asBox().getHalfExtents().ToVector3();
        }
        else if ( type == agxCollide.Shape.Type.CYLINDER ) {
          Debug.Log( "Shape.Type.CYLINDER" );
        }
        else if ( type == agxCollide.Shape.Type.CAPSULE ) {
          Debug.Log( "Shape.Type.CAPSULE" );
        }
        else if ( type == agxCollide.Shape.Type.SPHERE ) {
          Debug.Log( "Shape.Type.SHERE" );
        }
        else if ( type == agxCollide.Shape.Type.PLANE ) {
          Debug.Log( "Shape.Type.PLANE" );
        }
        else if ( type == agxCollide.Shape.Type.CONVEX ) {
          Debug.Log( "Shape.Type.CONVEX" );
        }
        else if ( type == agxCollide.Shape.Type.TRIMESH ) {
          Debug.Log( "Shape.Type.TRIMESH" );
        }
        else {
          Debug.LogWarning( "Unsupported shape type: " + type );
          continue;
        }

        if ( nativeShape.getRenderData() != null ) {
          var renderData = nativeShape.getRenderData();
          var renderer = shapeGameObject.AddComponent<MeshRenderer>();
          var filter = shapeGameObject.AddComponent<MeshFilter>();
          var toLocal = shapeGameObject.transform.worldToLocalMatrix;
          var mesh = new Mesh();

          // Assigning and converting vertices.
          // Note: RenderData vertices assumed to be given in world coordinates.
          mesh.SetVertices( ( from v
                              in renderData.getVertexArray()
                              select toLocal.MultiplyPoint( v.ToHandedVector3() ) ).ToList() );

          // Assigning and converting colors.
          mesh.SetColors( ( from c
                            in renderData.getColorArray()
                            select c.ToColor() ).ToList() );

          // Assigning and converting normals.
          mesh.SetNormals( ( from n
                             in renderData.getNormalArray()
                             select n.ToHandedVector3() ).ToList() );

          // Unsure about this one.
          mesh.SetUVs( 0,
                       ( from uv
                         in renderData.getTexCoordArray()
                         select uv.ToVector2() ).ToList() );

          // Converting counter clockwise -> clockwise.
          var triangles = new List<int>();
          var indexArray = renderData.getIndexArray();
          for ( int i = 0; i < indexArray.Count; i += 3 ) {
            triangles.Add( Convert.ToInt32( indexArray[ i + 0 ] ) );
            triangles.Add( Convert.ToInt32( indexArray[ i + 2 ] ) );
            triangles.Add( Convert.ToInt32( indexArray[ i + 1 ] ) );
          }
          mesh.SetTriangles( triangles, 0, false );

          mesh.RecalculateBounds();
          mesh.RecalculateNormals();
          mesh.RecalculateTangents();

          string dataPath = prefabPath + "/Data";
          if ( !Directory.Exists( prefabFile.DirectoryName + "/Data" ) )
            dataPath = AssetDatabase.GUIDToAssetPath( AssetDatabase.CreateFolder( prefabPath, "Data" ) );

          var shader = Shader.Find( "Standard" ) ?? Shader.Find( "Diffuse" );
          if ( shader == null )
            Debug.LogError( "Unable to find shader." );

          var renderMaterial = renderData.getRenderMaterial();
          var material = new Material( shader );
          material.color = renderMaterial.getDiffuseColor().ToColor();
          AssetDatabase.CreateAsset( mesh, dataPath + "/" + prefabName + "_mesh.asset" );
          AssetDatabase.CreateAsset( material, dataPath + "/" + prefabName + "_material.asset" );

          filter.sharedMesh = mesh;
          renderer.sharedMaterial = material;

          AssetDatabase.SaveAssets();
          AssetDatabase.Refresh();
        }
      }
    }

    private static void ReadAGXFile( FileInfo file )
    {
      try {
        using ( var inputFile = new IO.InputAGXFile( file ) ) {
          inputFile.TryLoad();
          inputFile.TryParse();
          inputFile.TryGenerate();
        }
      }
      catch ( System.Exception e ) {
        Debug.LogException( e );
      }

      //if ( !file.Exists ) {
      //  Debug.LogWarning( "Unable to find file: " + file.FullName );
      //  Debug.Log( "Directory full name: " + file.Directory.FullName );
      //  Debug.Log( "Prefab name:         " + file.Directory.FullName + @"/" + Path.GetFileNameWithoutExtension( file.Name ) + @".prefab" );
      //  return;
      //}

      //var prefabName      = Path.GetFileNameWithoutExtension( file.Name );
      //FileInfo prefabFile = new FileInfo( file.Directory.FullName + @"\" + prefabName + @".prefab" );
      //// TODO: Find a more safe way to determine the relative directory to Application.dataPath.
      //var prefabPath      = prefabFile.DirectoryName.Substring( file.Directory.FullName.IndexOf( "Assets" ) ).Replace( '\\', '/' );
      //var prefabFilename  = prefabPath + "/" + prefabFile.Name;

      //GameObject parent = new GameObject( Path.GetFileNameWithoutExtension( file.Name ) );
      //try {
      //  using ( var simulation = new agxSDK.Simulation() ) {
      //    Debug.Log( "Loading: " + file.FullName );
      //    if ( !agxIO.agxIOSWIG.readFile( file.FullName, simulation ) )
      //      throw new AgXUnity.Exception( "Unable to read: " + file.FullName + " into a simulation. Ignoring file." );

      //    Debug.Log( "Successfully loaded file." );
      //    Debug.Log( "#bodies:      " + simulation.getRigidBodies().Count );
      //    Debug.Log( "#constraints: " + simulation.getConstraints().Count );

      //    parent.transform.position = Vector3.zero;
      //    parent.transform.rotation = Quaternion.identity;
      //    foreach ( var nativeRb in simulation.getRigidBodies() ) {
      //      GameObject rbGameObject = new GameObject( nativeRb.getName() );
      //      parent.AddChild( rbGameObject );
      //      rbGameObject.transform.position = nativeRb.getPosition().ToHandedVector3();
      //      rbGameObject.transform.rotation = nativeRb.getRotation().ToHandedQuaternion();
      //      rbGameObject.AddComponent<RigidBody>().RestoreLocalDataFrom( nativeRb.get() );

      //      foreach ( var nativeGeometry in nativeRb.getGeometries() ) {
      //        ReadGeometry( nativeGeometry.get(), rbGameObject, prefabFile, prefabPath, prefabName );
      //      }
      //    }

      //    foreach ( var nativeGeometry in simulation.getGeometries() ) {
      //      if ( nativeGeometry.getRigidBody() != null )
      //        continue;

      //      ReadGeometry( nativeGeometry.get(), parent, prefabFile, prefabPath, prefabName );
      //    }

      //    var prefab = PrefabUtility.CreateEmptyPrefab( prefabFilename );
      //    if ( prefab == null )
      //      throw new AgXUnity.Exception( "CreateEmptyPrefab returned null: " + prefabFilename );
      //    PrefabUtility.ReplacePrefab( parent, prefab );
      //  }
      //}
      //catch ( System.Exception e ) {
      //  Debug.LogException( e );
      //}

      //if ( parent != null )
      //  GameObject.DestroyImmediate( parent );
    }
  }
}
