using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;
using AgXUnity.Rendering;
using Tree = AgXUnityEditor.IO.InputAGXFileTree;
using Node = AgXUnityEditor.IO.InputAGXFileTreeNode;

// TODO:   RestoredAGXFile tool.
//         Constraint animation.
//       v HeightField (partial, converts to mesh)

namespace AgXUnityEditor.IO
{
  /// <summary>
  /// Load .agx/.aagx file to an prefab with the same name in the same directory.
  /// 1. TryLoad - loading file into native simulation (restoring file)
  /// 2. TryParse - parsing simulation creating the simulation tree
  /// 3. TryGenerate - generates game objects and assets given simulation tree
  /// 4. TryCreatePrefab - creates a prefab of the generated objects, the instance
  ///                      is destroyed when this object is disposed
  /// </summary>
  public class InputAGXFile : IDisposable
  {
    /// <summary>
    /// AGX file info.
    /// </summary>
    public AGXFileInfo FileInfo { get; private set; }

    /// <summary>
    /// Native simulation the file is loaded into.
    /// </summary>
    public agxSDK.Simulation Simulation { get; private set; }

    /// <summary>
    /// Parent game object the simulation tree adds objects to. This
    /// object is destroyed when this object is disposed.
    /// </summary>
    public GameObject Parent { get; private set; }

    /// <summary>
    /// True when the prefab has been successfully created.
    /// </summary>
    public bool Successful { get; private set; }

    /// <summary>
    /// Construct given AGX file info.
    /// </summary>
    /// <param name="info">AGX file info.</param>
    public InputAGXFile( AGXFileInfo info )
    {
      FileInfo = info ?? throw new ArgumentNullException( "info", "File info object is null." );

      if ( !FileInfo.Exists )
        throw new FileNotFoundException( "File not found: " + FileInfo.FullName );

      if ( FileInfo.Type == AGXFileInfo.FileType.Unknown || FileInfo.Type == AGXFileInfo.FileType.AGXPrefab )
        throw new AgXUnity.Exception( "Unsupported file format: " + FileInfo.FullName );

      Successful = false;
      Simulation = new agxSDK.Simulation();
    }

    /// <summary>
    /// Trying to read AGX file. Throws if something goes wrong.
    /// </summary>
    public void TryLoad()
    {
      m_progressBar = new ProgressBar() { Title = "Creating prefab from: " + FileInfo.NameWithExtension };

      using ( m_progressBar.Progress( "Loading: " + FileInfo.NameWithExtension, 1 ) )
        if ( !agxIO.agxIOSWIG.readFile( FileInfo.FullName, Simulation ) )
          throw new AgXUnity.Exception( "Unable to load file:" + FileInfo.FullName );
    }

    /// <summary>
    /// Trying to parse the simulation, creating the simulation tree.
    /// Throws if something goes wrong.
    /// </summary>
    public void TryParse()
    {
      using ( m_progressBar.Progress( "Parsing: " + FileInfo.NameWithExtension, 1 ) )
        m_tree.Parse( Simulation );
    }

    /// <summary>
    /// Trying to generate the objects given the simulation tree.
    /// Throws if something goes wrong.
    /// </summary>
    public void TryGenerate()
    {
      FileInfo.GetOrCreateDataDirectory();

      // Adding one for disabled collisions.
      int numSubProgresses = m_tree.Roots.Length + 1;
      using ( var subProgress = m_progressBar.Progress( "Generating: " + FileInfo.NameWithExtension, numSubProgresses ) ) {
        Parent                    = new GameObject( FileInfo.Name );
        Parent.transform.position = Vector3.zero;
        Parent.transform.rotation = Quaternion.identity;
        var fileData              = Parent.AddComponent<AgXUnity.IO.RestoredAGXFile>();

        foreach ( var root in m_tree.Roots ) {
          subProgress.Tick( root.Name == string.Empty ? root.Type.ToString() : root.Name );
          Generate( root );
          subProgress.Tack();
        }

        subProgress.Tick( "Disabled collisions" );
        var disabledCollisionsState = Simulation.getSpace().findDisabledCollisionsState();
        foreach ( var namePair in disabledCollisionsState.getDisabledNames() )
          fileData.AddDisabledPair( namePair.first, namePair.second );
        foreach ( var idPair in disabledCollisionsState.getDisabledIds() )
          fileData.AddDisabledPair( idPair.first.ToString(), idPair.second.ToString() );
        foreach ( var geometryPair in disabledCollisionsState.getDisabledGeometyPairs() ) {
          if ( !Tree.IsValid( geometryPair.first ) || !Tree.IsValid( geometryPair.second ) )
            continue;

          var geometry1Node = m_tree.GetNode( geometryPair.first.getUuid() );
          var geometry2Node = m_tree.GetNode( geometryPair.second.getUuid() );
          if ( geometry1Node == null || geometry2Node == null ) {
            Debug.LogWarning( "Unreferenced geometry in disabled collisions pair." );
            continue;
          }

          var geometry1Id = geometry2Node.Uuid.str();
          foreach ( var shapeNode in geometry1Node.GetChildren( Node.NodeType.Shape ) )
            shapeNode.GameObject.GetOrCreateComponent<CollisionGroups>().AddGroup( geometry1Id, false );
          var geometry2Id = geometry1Node.Uuid.str();
          foreach ( var shapeNode in geometry2Node.GetChildren( Node.NodeType.Shape ) )
            shapeNode.GameObject.GetOrCreateComponent<CollisionGroups>().AddGroup( geometry2Id, false );

          fileData.AddDisabledPair( geometry1Id, geometry2Id );
        }
        subProgress.Tack();
      }
    }

    /// <summary>
    /// Trying to create and save a prefab given the generated object(s).
    /// Throws if something goes wrong.
    /// </summary>
    /// <returns>Prefab parent.</returns>
    public UnityEngine.Object TryCreatePrefab()
    {
      using ( m_progressBar.Progress( "Creating prefab and saving assets.", 1 ) ) {
        if ( FileInfo.CreatePrefab( Parent ) == null )
          throw new AgXUnity.Exception( "Unable to create prefab: " + FileInfo.PrefabPath );

        FileInfo.Save();
      }

      Successful = true;

      return FileInfo.Parent;
    }

    /// <summary>
    /// Disposes the native simulation and destroys any created instances
    /// that hasn't been saved as assets.
    /// </summary>
    public void Dispose()
    {
      if ( Simulation != null )
        Simulation.Dispose();
      Simulation = null;

      if ( Parent != null )
        GameObject.DestroyImmediate( Parent );

      if ( m_progressBar != null )
        m_progressBar.Dispose();
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

            node.GameObject.AddComponent<Assembly>();

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

            FileInfo.AddAsset( node.Asset );

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
              FileInfo.AddAsset( frictionModelAsset );
              contactMaterial.FrictionModel = frictionModelAsset;
            }

            FileInfo.AddAsset( node.Asset );

            break;
          case Node.NodeType.Constraint:
            var nativeConstraint = m_tree.GetConstraint( node.Uuid );
            node.GameObject      = new GameObject( FindName( nativeConstraint.getName(),
                                                             "AgXUnity." + Constraint.FindType( nativeConstraint ) ) );
            Add( node );

            if ( !CreateConstraint( node ) )
              GameObject.DestroyImmediate( node.GameObject );

            break;
          case Node.NodeType.Wire:
            var nativeWire = m_tree.GetWire( node.Uuid );
            node.GameObject = new GameObject( FindName( nativeWire.getName(), "AgXUnity.Wire" ) );

            Add( node );

            if ( !CreateWire( node ) )
              GameObject.DestroyImmediate( node.GameObject );

            break;
          case Node.NodeType.Cable:
            var nativeCable = m_tree.GetCable( node.Uuid );
            node.GameObject = new GameObject( FindName( nativeCable.getName(), "AgXUnity.Cable" ) );

            Add( node );

            if ( !CreateCable( node ) )
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

    private class SubMeshData
    {
      private Dictionary<Vector3, int> m_vertexToIndexTable = new Dictionary<Vector3, int>();
      private List<Vector3> m_vertices = new List<Vector3>();
      private List<int> m_indices = new List<int>();

      public int NumVertices { get { return m_vertices.Count; } }
      public int NumIndices { get { return m_indices.Count; } }

      public SubMeshData()
      {
        m_vertices.Capacity = Int16.MaxValue;
        m_indices.Capacity = Int16.MaxValue;
      }

      public void Add( Vector3 v1, Vector3 v2, Vector3 v3 )
      {
        Add( v1 );
        Add( v2 );
        Add( v3 );
      }

      public void Apply( ref Mesh mesh )
      {
        mesh.SetVertices( m_vertices );
        mesh.SetTriangles( m_indices, 0, false );

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
      }

      private void Add( Vector3 v )
      {
        int index;
        if ( !m_vertexToIndexTable.TryGetValue( v, out index ) ) {
          index = m_vertices.Count;
          m_vertexToIndexTable.Add( v, index );
          m_vertices.Add( v );
        }

        m_indices.Add( index );
      }
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
                nativeShapeType == agxCollide.Shape.Type.TRIMESH ||
                nativeShapeType == agxCollide.Shape.Type.HEIGHT_FIELD ) {
        var mesh          = node.GameObject.AddComponent<AgXUnity.Collide.Mesh>();
        var collisionData = nativeShape.asMesh().getMeshData();
        var nativeToWorld = nativeShape.getTransform();
        var meshToLocal   = mesh.transform.worldToLocalMatrix;

        if ( collisionData.getVertices().Count > UInt16.MaxValue ) {
          var nativeVertices = collisionData.getVertices();
          var nativeIndicies = collisionData.getIndices();
          var splitter       = MeshSplitter.Split( nativeVertices,
                                                   nativeIndicies,
                                                   v =>
                                                     meshToLocal.MultiplyPoint3x4( nativeToWorld.preMult( v ).ToHandedVector3() ) );
          var subMeshes      = splitter.Meshes;
          for ( int i = 0; i < subMeshes.Length; ++i ) {
            subMeshes[ i ].name = "Mesh_" + mesh.name + ( i == 0 ? "" : "_Sub_" + i.ToString() );
            FileInfo.AddAsset( subMeshes[ i ] );
            mesh.AddSourceObject( subMeshes[ i ] );
          }
        }
        else {
          var source = new Mesh();
          source.name = "Mesh_" + mesh.name;

          source.SetVertices( ( from v
                                in collisionData.getVertices()
                                select meshToLocal.MultiplyPoint3x4( nativeToWorld.preMult( v ).ToHandedVector3() ) ).ToList() );

          // Converting counter clockwise -> clockwise.
          var triangles      = new List<int>();
          var indexArray     = collisionData.getIndices();
          triangles.Capacity = indexArray.Count;
          for ( int i = 0; i < indexArray.Count; i += 3 ) {
            triangles.Add( Convert.ToInt32( indexArray[ i + 0 ] ) );
            triangles.Add( Convert.ToInt32( indexArray[ i + 2 ] ) );
            triangles.Add( Convert.ToInt32( indexArray[ i + 1 ] ) );
          }
          source.SetTriangles( triangles, 0, false );

          source.RecalculateBounds();
          source.RecalculateNormals();
          source.RecalculateTangents();

          FileInfo.AddAsset( source );

          mesh.SetSourceObject( source );
        }
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

      // Groups referenced in geometry node.
      var groups = node.Parent.GetReferences( Node.NodeType.GroupId );
      if ( groups.Length > 0 ) {
        var groupsComponent = shape.gameObject.GetOrCreateComponent<CollisionGroups>();
        foreach ( var group in groups )
          if ( group.Object is string )
            groupsComponent.AddGroup( group.Object as string, false );
      }

      if ( nativeShape.getRenderData() != null )
        CreateRenderData( node );

      return true;
    }

    private bool CreateRenderData( Node node )
    {
      var nativeShape = m_tree.GetShape( node.Uuid );
      var renderData  = nativeShape.getRenderData();
      if ( renderData == null || !renderData.getShouldRender() )
        return false;

      var nativeGeometry = m_tree.GetGeometry( node.Parent.Uuid );
      var shape          = node.GameObject.GetComponent<AgXUnity.Collide.Shape>();

      var toWorld  = nativeGeometry.getTransform();
      var toLocal  = shape.transform.worldToLocalMatrix;

      var meshes = new Mesh[] { };
      if ( renderData.getVertexArray().Count > UInt16.MaxValue ) {
        Debug.LogWarning( "Render data contains more than " +
                          UInt16.MaxValue +
                          " vertices. Splitting it into smaller meshes." );

        var splitter = MeshSplitter.Split( renderData.getVertexArray(),
                                           renderData.getIndexArray(),
                                           v => toLocal.MultiplyPoint3x4( toWorld.preMult( v ).ToHandedVector3() ) );
        meshes = splitter.Meshes;
      }
      else {
        var mesh     = new Mesh();
        mesh.name    = shape.name + "_Visual_Mesh";

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

        meshes = new Mesh[] { mesh };
      }

      var shader = Shader.Find( "Standard" ) ?? Shader.Find( "Diffuse" );
      if ( shader == null )
        Debug.LogError( "Unable to find standard shaders." );

      var renderMaterial = renderData.getRenderMaterial();
      var material       = new Material( shader );
      material.name      = shape.name + "_Visual_Material";

      if ( renderMaterial.hasDiffuseColor() ) {
        var color = renderMaterial.getDiffuseColor().ToColor();
        color.a = 1.0f - renderMaterial.getTransparency();

        material.SetVector( "_Color", color );
      }
      if ( renderMaterial.hasEmissiveColor() )
        material.SetVector( "_EmissionColor", renderMaterial.getEmissiveColor().ToColor() );

      material.SetFloat( "_Metallic", 0.3f );
      material.SetFloat( "_Glossiness", 0.8f );

      if ( renderMaterial.getTransparency() > 0.0f )
        material.SetBlendMode( BlendMode.Transparent );

      FileInfo.AddAsset( material );
      foreach ( var mesh in meshes )
        FileInfo.AddAsset( mesh );

      ShapeVisual.CreateRenderData( shape, meshes, material );

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
      constraint.SetType( constraintType );

      try {
        constraint.TryAddElementaryConstraints( nativeConstraint );
        constraint.VerifyImplementation();
      }
      catch ( System.Exception e ) {
        Debug.LogException( e );
        return false;
      }

      // Scaling damping to our (sigh) hard coded time step.
      float fixedStepTime = AgXUnity.Simulation.DefaultTimeStep;
      float readTimeStep  = Convert.ToSingle( Simulation.getTimeStep() );
      float timeStepRatio = fixedStepTime / readTimeStep;
      if ( !Mathf.Approximately( timeStepRatio, 1.0f ) ) {
        foreach ( var ec in constraint.ElementaryConstraints ) {
          foreach ( var rowData in ec.RowData ) {
            if ( rowData.Compliance < -float.Epsilon ) {
              Debug.LogWarning( "Constraint: " + constraint.name +
                                " (ec name: " + rowData.ElementaryConstraint.NativeName + ")," +
                                " has too low compliance: " + rowData.Compliance + ". Setting to zero." );
              rowData.Compliance = 0.0f;
            }
            else if ( rowData.Compliance > float.MaxValue ) {
              Debug.LogWarning( "Constraint: " + constraint.name +
                                " (ec name: " + rowData.ElementaryConstraint.NativeName + ")," +
                                " has too high compliance: " + rowData.Compliance + ". Setting to a large value." );
              rowData.Compliance = 0.5f * float.MaxValue;
            }
            rowData.Damping *= timeStepRatio;
          }
        }
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

    private bool CreateWire( Node node )
    {
      var nativeWire = m_tree.GetWire( node.Uuid );
      if ( nativeWire == null ) {
        Debug.LogWarning( "Unable to find native instance of wire: " + node.GameObject.name +
                          " (UUID: " + node.Uuid.str() + ")" );
        return false;
      }

      Func<agx.RigidBody, GameObject> findRigidBody = ( nativeRb ) =>
      {
        if ( nativeRb == null )
          return null;

        // Do not reference lumped nodes!
        if ( agxWire.Wire.isLumpedNode( nativeRb ) )
          return null;

        Node rbNode = m_tree.GetNode( nativeRb.getUuid() );
        if ( rbNode == null ) {
          Debug.LogWarning( "Unable to find reference rigid body: " + nativeRb.getName() + " (UUID: " + nativeRb.getUuid().str() + ")" );
          return null;
        }
        if ( rbNode.GameObject == null ) {
          Debug.LogWarning( "Referenced native rigid body hasn't a game object: " + nativeRb.getName() + " (UUID: " + rbNode.Uuid.str() + ")" );
          return null;
        }

        return rbNode.GameObject;
      };

      var wire  = node.GameObject.AddComponent<Wire>();
      var route = wire.Route;

      wire.RestoreLocalDataFrom( nativeWire );

      var nativeIt         = nativeWire.getRenderBeginIterator();
      var nativeEndIt      = nativeWire.getRenderEndIterator();
      var nativeBeginWinch = nativeWire.getWinchController( 0u );
      var nativeEndWinch   = nativeWire.getWinchController( 1u );

      if ( nativeBeginWinch != null ) {
        route.Add( nativeBeginWinch,
                   findRigidBody( nativeBeginWinch.getRigidBody() ) );
      }
      // Connecting nodes will show up in render iterators.
      else if ( nativeIt.get().getNodeType() != agxWire.Node.Type.CONNECTING && nativeWire.getFirstNode().getNodeType() == agxWire.Node.Type.BODY_FIXED )
        route.Add( nativeWire.getFirstNode(), findRigidBody( nativeWire.getFirstNode().getRigidBody() ) );

      while ( !nativeIt.EqualWith( nativeEndIt ) ) {
        var nativeNode = nativeIt.get();
        route.Add( nativeNode, findRigidBody( nativeNode.getRigidBody() ) );
        nativeIt.inc();
      }

      // Remove last node if we should have a winch or a body fixed node there.
      if ( route.Last().Type == Wire.NodeType.FreeNode && nativeWire.getLastNode().getNodeType() == agxWire.Node.Type.BODY_FIXED )
        route.Remove( route.Last() );

      if ( nativeEndWinch != null ) {
        route.Add( nativeEndWinch,
                   findRigidBody( nativeEndWinch.getRigidBody() ) );
      }
      else if ( nativeIt.prev().get().getNodeType() != agxWire.Node.Type.CONNECTING && nativeWire.getLastNode().getNodeType() == agxWire.Node.Type.BODY_FIXED )
        route.Add( nativeWire.getLastNode(), findRigidBody( nativeWire.getLastNode().getRigidBody() ) );

      var materials = node.GetReferences( Node.NodeType.Material );
      if ( materials.Length > 0 )
        wire.Material = materials[ 0 ].Asset as ShapeMaterial;

      wire.GetComponent<AgXUnity.Rendering.WireRenderer>().InitializeRenderer();
      // Reset to assign default material.
      wire.GetComponent<AgXUnity.Rendering.WireRenderer>().Material = null;

      // Adding collision group from restored instance since the disabled pair
      // will be read from Space (wire.setEnableCollisions( foo, false ) will
      // work out of the box).
      var collisionGroups = wire.gameObject.GetOrCreateComponent<CollisionGroups>();
      collisionGroups.AddGroup( nativeWire.getGeometryController().getDisabledCollisionsGroupId().ToString(), false );
      foreach ( var id in nativeWire.getGeometryController().getGroupIds() )
        collisionGroups.AddGroup( id.ToString(), false );

      return true;
    }

    private bool CreateCable( Node node )
    {
      var nativeCable = m_tree.GetCable( node.Uuid );
      if ( nativeCable == null ) {
        Debug.LogWarning( "Unable to find native instance of cable: " + node.GameObject.name +
                          " (UUID: " + node.Uuid.str() + ")" );
        return false;
      }

      var cable = node.GameObject.AddComponent<Cable>();
      var route = cable.Route;

      cable.RestoreLocalDataFrom( nativeCable );
      cable.RouteAlgorithm = Cable.RouteType.Identity;

      var properties  = ScriptAsset.Create<CableProperties>();
      properties.name = cable.name + "_properties";

      properties.RestoreLocalDataFrom( nativeCable.getCableProperties(), nativeCable.getCablePlasticity() );

      FileInfo.AddAsset( properties );

      cable.Properties = properties;

      for ( var it = nativeCable.getSegments().begin(); !it.EqualWith( nativeCable.getSegments().end() ); it.inc() ) {
        var segment = it.get();
        route.Add( segment, attachment =>
                            {
                              if ( attachment.getRigidBody() == null )
                                return null;
                              var rbNode = m_tree.GetNode( attachment.getRigidBody().getUuid() );
                              if ( rbNode == null ) {
                                Debug.LogWarning( "Unable to find rigid body in cable attachment." );
                                return null;
                              }
                              return rbNode.GameObject;
                            } );
      }

      var materials = node.GetReferences( Node.NodeType.Material );
      if ( materials.Length > 0 )
        cable.Material = materials[ 0 ].Asset as ShapeMaterial;

      cable.GetComponent<AgXUnity.Rendering.CableRenderer>().InitializeRenderer();
      cable.GetComponent<AgXUnity.Rendering.CableRenderer>().Material = null;

      // Adding collision group from restored instance since the disabled pair
      // will be read from Space (cable.setEnableCollisions( foo, false ) will
      // work out of the box).
      var collisionGroups = cable.gameObject.GetOrCreateComponent<CollisionGroups>();
      collisionGroups.AddGroup( nativeCable.getUniqueId().ToString(), false );
      var referencedGroups = node.GetReferences( Node.NodeType.GroupId );
      foreach ( var group in referencedGroups )
        if ( group.Object is string )
          collisionGroups.AddGroup( group.Object as string, false );

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

    internal class SubProgress : IDisposable
    {
      public SubProgress( ProgressBar progressBar, ProgressBar.StateType target, string name, int numSubSteps )
      {
        m_progressBar = progressBar;
        m_target = target;
        m_name = name;
        m_numSubSteps = numSubSteps;

        ShowProgress( name );
      }

      public void Dispose()
      {
        m_progressBar.Progress();
      }

      public void Tick( string name )
      {
        ShowProgress( name );
      }

      public void Tack()
      {
        ++m_counter;
      }

      private void ShowProgress( string name )
      {
        EditorUtility.DisplayProgressBar( m_progressBar.Title,
                                          name,
                                          m_progressBar.GetSubProgressStart( m_target - 1 ) + ( (float)m_counter / m_numSubSteps ) * m_progressBar.GetSubProgressDelta( m_target ) );
      }

      private ProgressBar m_progressBar = null;
      private ProgressBar.StateType m_target = ProgressBar.StateType.Initial;
      private int m_numSubSteps = 1;
      private string m_name = string.Empty;
      private int m_counter = 0;
    }

    internal class ProgressBar : IDisposable
    {
      public enum StateType
      {
        Initial,
        ReadingFile,
        CreatingSimulationTree,
        GeneratingObjects,
        CreatingPrefab,
        Done
      }

      public string Title = "";

      public SubProgress Progress( string name, int numSubSteps )
      {
        return new SubProgress( this, m_state + 1, name, numSubSteps );
      }

      public void Progress()
      {
        if ( m_state == StateType.Done )
          return;

        m_state = m_state + 1;
      }

      public void Dispose()
      {
        EditorUtility.ClearProgressBar();
      }

      public float GetSubProgressStart( StateType start )
      {
        return m_subProgress[ (int)start ];
      }

      public float GetSubProgressDelta( StateType target )
      {
        if ( target == StateType.Initial )
          return 0.0f;
        return m_subProgress[ (int)target ] - m_subProgress[ (int)( target - 1 ) ];
      }

      private StateType m_state = StateType.Initial;
      //                                            Init, Read, Create, Gen, Prefab, Done
      private float[] m_subProgress = new float[] { 0.0f, 0.33f, 0.4f, 0.95f, 1.0f, 1.0f };
    }

    private Tree m_tree = new Tree();
    private HashSet<string> m_names = new HashSet<string>();
    private ProgressBar m_progressBar = null;
  }
}
