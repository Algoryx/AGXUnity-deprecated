using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;
using Tree = AgXUnityEditor.IO.InputAGXFileTree;
using Node = AgXUnityEditor.IO.InputAGXFileTreeNode;

namespace AgXUnityEditor.IO
{
  // TODO: Disable collisions (complete shape/geometry)
  // TODO: Disable collisions between objects
  // TODO: Group id and space disabled pairs.
  // TODO: OnPrefabAddedToScene directory hell cleanup.
  public class InputAGXFile : IDisposable
  {
    public static string MakeRelative( string full, string root )
    {
      Uri fullUri = new Uri( full );
      Uri rootUri = new Uri( root );
      Uri relUri = rootUri.MakeRelativeUri( fullUri );
      return relUri.ToString();
    }

    public string Name { get; private set; }

    public string RelDirectoryPath { get; private set; }

    public string RelPrefabDataPath { get; private set; }

    public FileInfo AGXFileInfo { get; private set; }

    public string PrefabFilename { get; private set; }

    public agxSDK.Simulation Simulation { get; private set; }

    public GameObject Parent { get; private set; }

    public bool Successful { get; private set; }

    public InputAGXFile( FileInfo file )
    {
      if ( file == null )
        throw new ArgumentNullException( "file", "File info object is null." );

      if ( !file.Exists )
        throw new FileNotFoundException( "File not found: " + file.FullName );

      if ( file.Extension != ".agx" && file.Extension != ".aagx" )
        throw new AgXUnity.Exception( "Unsupported file format: " + file.Extension + " (file: " + file.FullName + ")" );

      AGXFileInfo       = file;
      Name              = Path.GetFileNameWithoutExtension( AGXFileInfo.Name );
      RelDirectoryPath  = MakeRelative( AGXFileInfo.Directory.FullName, Application.dataPath ).Replace( '\\', '/' );
      RelPrefabDataPath = RelDirectoryPath + "/" + Name + "_Data";
      PrefabFilename    = RelDirectoryPath + "/" + Name + ".prefab";

      Successful = false;
      Simulation = new agxSDK.Simulation();
    }

    public void TryLoad()
    {
      using ( new TimerBlock( "Loading: " + AGXFileInfo.Name ) )
        if ( !agxIO.agxIOSWIG.readFile( AGXFileInfo.FullName, Simulation ) )
          throw new AgXUnity.Exception( "Unable to load " + AGXFileInfo.Extension + " file: " + AGXFileInfo.FullName );
    }

    public void TryParse()
    {
      using ( new TimerBlock( "Parsing: " + AGXFileInfo.Name ) )
        m_tree.Parse( Simulation );
    }

    public void TryGenerate()
    {
      if ( !Directory.Exists( AGXFileInfo.DirectoryName + "/" + Name + "_Data" ) )
        AssetDatabase.CreateFolder( RelDirectoryPath, Name + "_Data" );

      using ( new TimerBlock( "Generating: " + AGXFileInfo.Name ) ) {
        Parent                    = new GameObject( Name );
        Parent.transform.position = Vector3.zero;
        Parent.transform.rotation = Quaternion.identity;

        foreach ( var root in m_tree.Roots )
          Generate( root );
      }
    }

    public void TryCreatePrefab()
    {
      var prefab = PrefabUtility.CreateEmptyPrefab( PrefabFilename );
      if ( prefab == null )
        throw new AgXUnity.Exception( "Unable to create prefab: " + PrefabFilename );

      PrefabUtility.ReplacePrefab( Parent, prefab );

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Successful = true;
    }

    public void Dispose()
    {
      if ( Simulation != null )
        Simulation.Dispose();
      Simulation = null;

      if ( Parent != null )
        GameObject.DestroyImmediate( Parent );
    }

    private void Generate( Node node )
    {
      if ( node == null )
        return;

      if ( node.GameObject == null ) {
        switch ( node.Type ) {
          case Node.NodeType.Assembly:
            agx.Frame frame = m_tree.GetAssembly( node.Uuid );
            node.GameObject = new GameObject( FindName( "", node.Type.ToString() ) );
            Add( node );
            node.GameObject.transform.position = frame.getTranslate().ToHandedVector3();
            node.GameObject.transform.rotation = frame.getRotate().ToHandedQuaternion();
            break;
          case Node.NodeType.RigidBody:
            agx.RigidBody nativeRb = m_tree.GetRigidBody( node.Uuid );
            node.GameObject = new GameObject( FindName( nativeRb.getName(), node.Type.ToString() ) );
            Add( node );
            node.GameObject.transform.position = nativeRb.getPosition().ToHandedVector3();
            node.GameObject.transform.rotation = nativeRb.getRotation().ToHandedQuaternion();

            node.GameObject.AddComponent<RigidBody>().RestoreLocalDataFrom( nativeRb );

            break;
          case Node.NodeType.Geometry:
            // Ignoring geometries - handling Shape == Geometry.
            // The shapes are children to this node.
            break;
          case Node.NodeType.Shape:
            var nativeGeometry  = m_tree.GetGeometry( node.Parent.Uuid );
            var nativeShape     = m_tree.GetShape( node.Uuid );
            var nativeShapeType = (agxCollide.Shape.Type)nativeShape.getType();

            node.GameObject = new GameObject( FindName( nativeGeometry.getName() +
                                                        "_" +
                                                        nativeShapeType.ToString().ToLower().FirstCharToUpperCase(),
                                                        node.Type.ToString() ) );
            Add( node );
            node.GameObject.transform.position = nativeShape.getTransform().getTranslate().ToHandedVector3();
            node.GameObject.transform.rotation = nativeShape.getTransform().getRotate().ToHandedQuaternion();

            if ( !CreateShape( node ) )
              GameObject.DestroyImmediate( node.GameObject );

            break;
          case Node.NodeType.Material:
            var nativeMaterial = m_tree.GetMaterial( node.Uuid );
            node.Asset         = ScriptAsset.Create<ShapeMaterial>().RestoreLocalDataFrom( nativeMaterial );
            node.Asset.name    = FindName( nativeMaterial.getName(), node.Type.ToString() );
            AddAsset( node );
            break;
          case Node.NodeType.ContactMaterial:
            var nativeContactMaterial = m_tree.GetContactMaterial( node.Uuid );
            var nativeFrictionModel   = nativeContactMaterial.getFrictionModel();
            node.Asset                = ScriptAsset.Create<ContactMaterial>().RestoreLocalDataFrom( nativeContactMaterial );
            node.Asset.name           = FindName( "ContactMaterial_" +
                                                  nativeContactMaterial.getMaterial1().getName() +
                                                  "_" +
                                                  nativeContactMaterial.getMaterial2().getName(),
                                                  node.Type.ToString() );

            ContactMaterial contactMaterial = node.Asset as ContactMaterial;
            var materials = node.GetReferences( Node.NodeType.Material );
            if ( materials.Length == 0 )
              Debug.LogWarning( "No materials referenced to ContactMaterial node." );
            else if ( materials.Length == 1 )
              contactMaterial.Material1 = contactMaterial.Material2 = materials[ 0 ].Asset as ShapeMaterial;
            else if ( materials.Length > 1 ) {
              contactMaterial.Material1 = materials[ 0 ].Asset as ShapeMaterial;
              contactMaterial.Material2 = materials[ 1 ].Asset as ShapeMaterial;
              if ( materials.Length > 2 )
                Debug.LogWarning( "More than two materials referenced to ContactMaterial (" + node.Asset.name + "). First two are used." );
            }

            if ( nativeFrictionModel != null ) {
              var frictionModelAsset = ScriptAsset.Create<FrictionModel>().RestoreLocalDataFrom( nativeFrictionModel );
              frictionModelAsset.name = "FrictionModel_" + contactMaterial.name;
              AddAsset( frictionModelAsset );
              contactMaterial.FrictionModel = frictionModelAsset;
            }

            AddAsset( node );

            break;
          case Node.NodeType.Constraint:
            var nativeConstraint = m_tree.GetConstraint( node.Uuid );
            node.GameObject      = new GameObject( FindName( nativeConstraint.getName(),
                                                             "AgXUnity." + Constraint.FindType( nativeConstraint ) ) );
            Add( node );

            if ( !CreateConstraint( node ) )
              GameObject.DestroyImmediate( node.GameObject );

            break;
        }
      }

      foreach ( var child in node.Children )
        Generate( child );
    }

    private void Add( Node node )
    {
      if ( node.GameObject == null ) {
        Debug.LogWarning( "Trying to add node with null GameObject." );
        return;
      }

      // Passing parents with null game objects - e.g., shapes
      // has geometry as parent but we're not creating objects
      // for geometries.
      Node localParent = node.Parent;
      while ( localParent != null && localParent.GameObject == null )
        localParent = localParent.Parent;

      if ( localParent != null )
        localParent.GameObject.AddChild( node.GameObject );
      else
        Parent.AddChild( node.GameObject );
    }

    private void AddAsset( Node node )
    {
      if ( node.Asset == null ) {
        Debug.LogWarning( "Trying to add null reference asset." );
        return;
      }

      AddAsset( node.Asset );
    }

    private void AddAsset( UnityEngine.Object obj )
    {
      AssetDatabase.CreateAsset( obj, RelPrefabDataPath + "/" + obj.name + ".asset" );
    }

    private bool CreateShape( Node node )
    {
      var nativeGeometry  = m_tree.GetGeometry( node.Parent.Uuid );
      var nativeShape     = m_tree.GetShape( node.Uuid );
      var nativeShapeType = (agxCollide.Shape.Type)nativeShape.getType();

      if ( nativeShapeType == agxCollide.Shape.Type.BOX ) {
        node.GameObject.AddComponent<AgXUnity.Collide.Box>().HalfExtents = nativeShape.asBox().getHalfExtents().ToVector3();
      }
      else if ( nativeShapeType == agxCollide.Shape.Type.CYLINDER ) {
        var cylinder = node.GameObject.AddComponent<AgXUnity.Collide.Cylinder>();
        cylinder.Radius = Convert.ToSingle( nativeShape.asCylinder().getRadius() );
        cylinder.Height = Convert.ToSingle( nativeShape.asCylinder().getHeight() );
      }
      else if ( nativeShapeType == agxCollide.Shape.Type.CAPSULE ) {
        var capsule = node.GameObject.AddComponent<AgXUnity.Collide.Capsule>();
        capsule.Radius = Convert.ToSingle( nativeShape.asCapsule().getRadius() );
        capsule.Height = Convert.ToSingle( nativeShape.asCapsule().getHeight() );
      }
      else if ( nativeShapeType == agxCollide.Shape.Type.SPHERE ) {
        var sphere = node.GameObject.AddComponent<AgXUnity.Collide.Sphere>();
        sphere.Radius = Convert.ToSingle( nativeShape.asSphere().getRadius() );
      }
      else if ( nativeShapeType == agxCollide.Shape.Type.CONVEX ||
                nativeShapeType == agxCollide.Shape.Type.TRIMESH ) {
        var mesh = node.GameObject.AddComponent<AgXUnity.Collide.Mesh>();
        var collisionData = nativeShape.asMesh().getMeshData();
        var source = new Mesh();
        source.name = "Mesh_" + mesh.name;

        var nativeToWorld = nativeShape.getTransform();
        var meshToLocal = mesh.transform.worldToLocalMatrix;
        source.SetVertices( ( from v
                              in collisionData.getVertices()
                              select meshToLocal.MultiplyPoint3x4( nativeToWorld.preMult( v ).ToHandedVector3() ) ).ToList() );

        // Converting counter clockwise -> clockwise.
        var triangles = new List<int>();
        var indexArray = collisionData.getIndices();
        for ( int i = 0; i < indexArray.Count; i += 3 ) {
          triangles.Add( Convert.ToInt32( indexArray[ i + 0 ] ) );
          triangles.Add( Convert.ToInt32( indexArray[ i + 2 ] ) );
          triangles.Add( Convert.ToInt32( indexArray[ i + 1 ] ) );
        }
        source.SetTriangles( triangles, 0, false );

        source.RecalculateBounds();
        source.RecalculateNormals();
        source.RecalculateTangents();

        AddAsset( source );

        mesh.SourceObject = source;
      }
      else {
        Debug.LogWarning( "Unsupported shape type: " + nativeShapeType );
        return false;
      }

      var shape = node.GameObject.GetComponent<AgXUnity.Collide.Shape>();
      var shapeMaterials = node.GetReferences( Node.NodeType.Material );
      if ( shapeMaterials.Length > 0 ) {
        shape.Material = shapeMaterials[ 0 ].Asset as ShapeMaterial;
        if ( shapeMaterials.Length > 1 )
          Debug.LogWarning( "More than one material referenced to shape: " + node.GameObject.name );
      }

      shape.CollisionsEnabled = nativeGeometry.getEnableCollisions();

      if ( nativeShape.getRenderData() != null )
        CreateRenderData( node );

      return true;
    }

    private bool CreateRenderData( Node node )
    {
      var nativeShape = m_tree.GetShape( node.Uuid );
      var renderData  = nativeShape.getRenderData();
      if ( renderData == null )
        return false;

      var nativeGeometry       = m_tree.GetGeometry( node.Parent.Uuid );
      var shape                = node.GameObject.GetComponent<AgXUnity.Collide.Shape>();
      var renderDataGameObject = new GameObject( shape.name + "_Visual" );
      var shapeVisual          = renderDataGameObject.AddComponent<AgXUnity.Rendering.ShapeVisual>();

      shape.gameObject.AddChild( renderDataGameObject );
      renderDataGameObject.transform.localPosition = Vector3.zero;
      renderDataGameObject.transform.localRotation = Quaternion.identity;
      renderDataGameObject.transform.localScale    = Vector3.one;

      var renderer = shapeVisual.MeshRenderer;
      var filter   = shapeVisual.MeshFilter;
      var toWorld  = nativeGeometry.getTransform();
      var toLocal  = renderDataGameObject.transform.worldToLocalMatrix;
      var mesh     = new Mesh();
      mesh.name    = shapeVisual.name + "_Mesh";

      // Assigning and converting vertices.
      // Note: RenderData vertices assumed to be given in geometry coordinates.
      mesh.SetVertices( ( from v
                          in renderData.getVertexArray()
                          select toLocal.MultiplyPoint3x4( toWorld.preMult( v ).ToHandedVector3() ) ).ToList() );

      // Assigning and converting colors.
      mesh.SetColors( ( from c
                        in renderData.getColorArray()
                        select c.ToColor() ).ToList() );

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

      var shader = Shader.Find( "Standard" ) ?? Shader.Find( "Diffuse" );
      if ( shader == null )
        Debug.LogError( "Unable to find standard shaders." );

      var renderMaterial = renderData.getRenderMaterial();
      var material       = new Material( shader );
      material.name      = shapeVisual.name + "_Material";

      if ( renderMaterial.hasDiffuseColor() )
        material.SetVector( "_Color", renderMaterial.getDiffuseColor().ToColor() );
      if ( renderMaterial.hasEmissiveColor() )
        material.SetVector( "_EmissionColor", renderMaterial.getEmissiveColor().ToColor() );

      material.SetFloat( "_Metallic", 0.3f );
      material.SetFloat( "_Glossiness", 0.8f );

      AddAsset( mesh );
      AddAsset( material );

      filter.sharedMesh       = mesh;
      renderer.sharedMaterial = material;

      return true;
    }

    private bool CreateConstraint( Node node )
    {
      var nativeConstraint = m_tree.GetConstraint( node.Uuid );
      if ( nativeConstraint == null ) {
        Debug.LogWarning( "Unable to find native constraint instance with name: " +
                          node.GameObject.name +
                          " (UUID: " + node.Uuid.str() + ")" );
        return false;
      }

      var bodyNodes = node.GetReferences( Node.NodeType.RigidBody );
      if ( bodyNodes.Length < 1 || bodyNodes.Length > 2 ) {
        Debug.LogWarning( "Unsupported number of body references to constraint with name: " +
                          node.GameObject.name +
                          " (#bodies: " + bodyNodes.Length + ")" );
        return false;
      }

      var constraintType = Constraint.FindType( nativeConstraint );
      if ( constraintType == ConstraintType.Unknown ) {
        Debug.LogWarning( "Unknown/unsupported constraint type of constraint with name: " +
                          node.GameObject.name +
                          " (UUID: " + node.Uuid.str() + ")" );
        return false;
      }

      Constraint constraint = node.GameObject.AddComponent<Constraint>();

      try {
        // Patching non-serialized elementary constraint names.
        bool patchNames = ( nativeConstraint.getNumElementaryConstraints() > 0 && nativeConstraint.getElementaryConstraint( 0 ).getName() == "" ) ||
                          ( nativeConstraint.getNumSecondaryConstraints() > 0 && nativeConstraint.getSecondaryConstraint( 0 ).getName() == "" );
        if ( patchNames ) {
          bool patchSuccessful = true;
          Constraint tmp       = Constraint.Create( constraintType );
          var tmpElementaryConstraints = tmp.GetOrdinaryElementaryConstraints();
          var tmpSecondaryConstraints  = tmp.GetElementaryConstraintControllers();
          int numNativeElementaryConstraints = Convert.ToInt32( nativeConstraint.getNumElementaryConstraints() );
          int numNativeSecondaryConstraints  = Convert.ToInt32( nativeConstraint.getNumSecondaryConstraints() );
          // If #elementary and #secondary we assume we've a match.
          if ( tmpElementaryConstraints.Length == numNativeElementaryConstraints &&
               tmpSecondaryConstraints.Length == numNativeSecondaryConstraints ) {
            for ( int i = 0; i < numNativeElementaryConstraints; ++i )
              nativeConstraint.getElementaryConstraint( (ulong)i ).setName( tmpElementaryConstraints[ i ].NativeName );
            for ( int i = 0; i < numNativeSecondaryConstraints; ++i )
              nativeConstraint.getSecondaryConstraint( (ulong)i ).setName( tmpSecondaryConstraints[ i ].NativeName );
          }
          // We know how to patch hinge with mismatching secondary constraints.
          else if ( constraintType == ConstraintType.Hinge ) {
            // Swing joint version.
            if ( nativeConstraint.getNumElementaryConstraints() == 2ul ) {
              nativeConstraint.getElementaryConstraint( 0ul ).setName( "SR" );
              nativeConstraint.getElementaryConstraint( 1ul ).setName( "SW" );
            }
            // Dot1 version.
            else if ( nativeConstraint.getNumElementaryConstraints() == 3ul ) {
              nativeConstraint.getElementaryConstraint( 0ul ).setName( "SR" );
              nativeConstraint.getElementaryConstraint( 1ul ).setName( "D1_VN" );
              nativeConstraint.getElementaryConstraint( 2ul ).setName( "D1_UN" );
            }
            else {
              Debug.LogWarning( "Unknown version of hinge instance." );
              patchSuccessful = false;
            }

            for ( int i = 0; patchSuccessful && i < numNativeSecondaryConstraints; ++i )
              nativeConstraint.getSecondaryConstraint( (ulong)i ).setName( tmpSecondaryConstraints[ i ].NativeName );
          }
          else {
            Debug.LogWarning( "Unable to recover mismatching constraint configuration of constraint with name: " +
                              node.GameObject.name +
                              " (UUID: " + node.Uuid.str() + ")" );
            patchSuccessful = false;
          }

          try {
            constraint.TryAddElementaryConstraints( nativeConstraint );
          }
          catch ( System.Exception e ) {
            Debug.LogException( e );
            patchSuccessful = false;
          }

          if ( patchSuccessful && constraint.Type == ConstraintType.Hinge )
            constraint.AdoptToReferenceHinge( tmp );

          GameObject.DestroyImmediate( tmp.gameObject );

          if ( !patchSuccessful )
            return false;
        }
        else
          constraint.TryAddElementaryConstraints( nativeConstraint );
      }
      catch ( System.Exception e ) {
        Debug.LogException( e );
        return false;
      }

      constraint.AttachmentPair.ReferenceFrame.SetParent( bodyNodes[ 0 ].GameObject );
      constraint.AttachmentPair.ReferenceFrame.LocalPosition = nativeConstraint.getAttachment( 0ul ).getFrame().getLocalTranslate().ToHandedVector3();
      constraint.AttachmentPair.ReferenceFrame.LocalRotation = nativeConstraint.getAttachment( 0ul ).getFrame().getLocalRotate().ToHandedQuaternion();

      if ( bodyNodes.Length > 1 )
        constraint.AttachmentPair.ConnectedFrame.SetParent( bodyNodes[ 1 ].GameObject );
      constraint.AttachmentPair.ConnectedFrame.LocalPosition = nativeConstraint.getAttachment( 1ul ).getFrame().getLocalTranslate().ToHandedVector3();
      constraint.AttachmentPair.ConnectedFrame.LocalRotation = nativeConstraint.getAttachment( 1ul ).getFrame().getLocalRotate().ToHandedQuaternion();

      constraint.AttachmentPair.Synchronized = constraintType != ConstraintType.DistanceJoint;

      return true;
    }

    private string FindName( string name, string typeName )
    {
      if ( name == "" )
        name = typeName;

      string result = name;
      int counter = 1;
      while ( m_names.Contains( result ) )
        result = name + " (" + ( counter++ ) + ")";

      m_names.Add( result );

      return result;
    }

    private Tree m_tree = new Tree();
    private HashSet<string> m_names = new HashSet<string>();
  }
}
