using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NodeType = AgXUnityEditor.IO.InputAGXFileTreeNode.NodeType;
using Node = AgXUnityEditor.IO.InputAGXFileTreeNode;

namespace AgXUnityEditor.IO
{
  public class InputAGXFileTree
  {
    public static bool IsCableRigidBody( agx.RigidBody rb )
    {
      return rb != null &&
             rb.getGeometries().Count > 0 &&
             agxCable.Cable.getCableForGeometry( rb.getGeometries()[ 0 ].get() ) != null;
    }

    public static bool IsValid( agx.RigidBody rb )
    {
      return rb != null &&
             !agxWire.Wire.isLumpedNode( rb ) &&
             !IsCableRigidBody( rb );
    }

    public static bool IsValid( agxCollide.Geometry geometry )
    {
      return geometry != null &&
             agxWire.Wire.getWire( geometry ) == null &&
             agxCable.Cable.getCableForGeometry( geometry ) == null;
    }

    public static bool IsValid( agx.Constraint constraint )
    {
      return constraint != null &&
             constraint.getNumBodies() > 0ul;
    }

    public Node[] Roots { get { return m_roots.ToArray(); } }

    public Node[] Constraints { get { return m_constraintRoot.Children; } }

    public Node[] Materials { get { return m_materialRoot.Children; } }

    public Node[] ContactMaterials { get { return m_contactMaterialRoot.Children; } }

    public Node GetNode( agx.Uuid uuid )
    {
      Node node;
      if ( m_nodeCache.TryGetValue( uuid, out node ) )
        return node;
      return null;
    }

    public agx.Frame GetAssembly( agx.Uuid uuid )
    {
      agx.Frame frame;
      if ( m_assemblies.TryGetValue( uuid, out frame ) )
        return frame;
      return null;
    }

    public agx.RigidBody GetRigidBody( agx.Uuid uuid )
    {
      agx.RigidBody rb;
      if ( m_bodies.TryGetValue( uuid, out rb ) )
        return rb;
      return null;
    }

    public agxCollide.Geometry GetGeometry( agx.Uuid uuid )
    {
      agxCollide.Geometry geometry;
      if ( m_geometries.TryGetValue( uuid, out geometry ) )
        return geometry;
      return null;
    }

    public agx.Constraint GetConstraint( agx.Uuid uuid )
    {
      agx.Constraint constraint;
      if ( m_constraints.TryGetValue( uuid, out constraint ) )
        return constraint;
      return null;
    }

    public agx.Material GetMaterial( agx.Uuid uuid )
    {
      agx.Material material;
      if ( m_materials.TryGetValue( uuid, out material ) )
        return material;
      return null;
    }

    public agx.ContactMaterial GetContactMaterial( agx.Uuid uuid )
    {
      agx.ContactMaterial contactMaterial;
      if ( m_contactMaterials.TryGetValue( uuid, out contactMaterial ) )
        return contactMaterial;
      return null;
    }

    public void Parse( agxSDK.Simulation simulation )
    {
      if ( simulation == null )
        throw new ArgumentNullException( "simulation", "agxSDK.Simulation instance is null." );

      m_roots.Clear();
      m_nodeCache.Clear();

      // RigidBody nodes.
      foreach ( var nativeRb in simulation.getRigidBodies() ) {
        if ( !IsValid( nativeRb.get() ) )
          continue;

        var assemblyNode = TryGetOrCreateAssembly( nativeRb.getFrame() );
        var rbNode       = GetOrCreateRigidBody( nativeRb.get(), assemblyNode == null );
        if ( assemblyNode != null )
          assemblyNode.AddChild( rbNode );

        foreach ( var nativeGeometry in nativeRb.getGeometries() )
          Parse( nativeGeometry.get(), rbNode );
      }

      // Free Geometry nodes.
      foreach ( var nativeGeometry in simulation.getGeometries() ) {
        if ( !IsValid( nativeGeometry.get() ) )
          continue;

        // We already have a node for this from reading bodies.
        if ( nativeGeometry.getRigidBody() != null ) {
          if ( !m_nodeCache.ContainsKey( nativeGeometry.getUuid() ) )
            Debug.LogWarning( "Geometry with rigid body ignored but isn't in present in the tree. Name: " + nativeGeometry.getName() );
          continue;
        }

        Parse( nativeGeometry.get(), TryGetOrCreateAssembly( nativeGeometry.getFrame() ) );
      }

      // Constraint nodes.
      foreach ( var nativeConstraint in simulation.getConstraints() ) {
        if ( !IsValid( nativeConstraint.get() ) )
          continue;

        var constraintNode = GetOrCreateConstraint( nativeConstraint.get() );
        var rb1Node = nativeConstraint.getBodyAt( 0 ) != null ?
                        GetNode( nativeConstraint.getBodyAt( 0 ).getUuid() ) :
                        null;
        var rb2Node = nativeConstraint.getBodyAt( 1 ) != null ?
                        GetNode( nativeConstraint.getBodyAt( 1 ).getUuid() ) :
                        null;

        if ( rb1Node != null )
          constraintNode.AddReference( rb1Node );
        if ( rb2Node != null )
          constraintNode.AddReference( rb2Node );
      }

      var mm = simulation.getMaterialManager();
      foreach ( var m1 in m_materials.Values ) {
        foreach ( var m2 in m_materials.Values ) {
          var cm = mm.getContactMaterial( m1, m2 );
          if ( cm == null )
            continue;

          var cmNode = GetOrCreateContactMaterial( cm );
          cmNode.AddReference( GetNode( m1.getUuid() ) );
          cmNode.AddReference( GetNode( m2.getUuid() ) );
        }
      }
    }

    private void Parse( agxCollide.Geometry geometry, Node parent )
    {
      var geometryNode = GetOrCreateGeometry( geometry, parent == null );
      if ( parent != null )
        parent.AddChild( geometryNode );

      if ( geometry.getMaterial() != null ) {
        var materialNode = GetOrCreateMaterial( geometry.getMaterial() );
        geometryNode.AddReference( materialNode );
      }
    }

    private Node TryGetOrCreateAssembly( agx.Frame child )
    {
      agx.Frame parent = child?.getParent();
      // If parent has a rigid body 'child' is probably a geometry.
      if ( parent == null || parent.getRigidBody() != null )
        return null;

      return GetOrCreateAssembly( parent );
    }

    private Node GetOrCreateAssembly( agx.Frame frame )
    {
      return GetOrCreateNode( NodeType.Assembly,
                              frame.getUuid(),
                              true,
                              () => m_assemblies.Add( frame.getUuid(), frame ) );
    }

    private Node GetOrCreateRigidBody( agx.RigidBody rb, bool isRoot )
    {
      return GetOrCreateNode( NodeType.RigidBody,
                              rb.getUuid(),
                              isRoot,
                              () => m_bodies.Add( rb.getUuid(), rb ) );
    }

    private Node GetOrCreateGeometry( agxCollide.Geometry geometry, bool isRoot )
    {
      return GetOrCreateNode( NodeType.Geometry,
                              geometry.getUuid(),
                              isRoot,
                              () => m_geometries.Add( geometry.getUuid(), geometry ) );
    }

    private Node GetOrCreateConstraint( agx.Constraint constraint )
    {
      return GetOrCreateNode( NodeType.Constraint,
                              constraint.getUuid(),
                              true,
                              () => m_constraints.Add( constraint.getUuid(), constraint ) );
    }

    private Node GetOrCreateMaterial( agx.Material material )
    {
      return GetOrCreateNode( NodeType.Material,
                              material.getUuid(),
                              true,
                              () => m_materials.Add( material.getUuid(), material ) );
    }

    private Node GetOrCreateContactMaterial( agx.ContactMaterial contactMaterial )
    {
      return GetOrCreateNode( NodeType.ContactMaterial,
                              contactMaterial.getUuid(),
                              true,
                              () => m_contactMaterials.Add( contactMaterial.getUuid(), contactMaterial ) );
    }

    private Node GetOrCreateNode( NodeType type, agx.Uuid uuid, bool isRoot, Action onCreate )
    {
      if ( m_nodeCache.ContainsKey( uuid ) )
        return m_nodeCache[ uuid ];

      onCreate();

      return CreateNode( type, uuid, isRoot );
    }

    private Node CreateNode( NodeType type, agx.Uuid uuid, bool isRoot )
    {
      Node node = new Node() { Type = type, Uuid = uuid };
      if ( isRoot ) {
        if ( type == NodeType.Constraint )
          m_constraintRoot.AddChild( node );
        else if ( type == NodeType.Material )
          m_materialRoot.AddChild( node );
        else if ( type == NodeType.ContactMaterial )
          m_contactMaterialRoot.AddChild( node );
        else if ( m_roots.FindIndex( n => n.Uuid == uuid ) >= 0 )
          Debug.LogError( "Node already present as root." );
        else
          m_roots.Add( node );
      }

      m_nodeCache.Add( uuid, node );

      return node;
    }

    private Dictionary<agx.Uuid, Node>                m_nodeCache        = new Dictionary<agx.Uuid, Node>();
    private Dictionary<agx.Uuid, agx.Frame>           m_assemblies       = new Dictionary<agx.Uuid, agx.Frame>();
    private Dictionary<agx.Uuid, agx.RigidBody>       m_bodies           = new Dictionary<agx.Uuid, agx.RigidBody>();
    private Dictionary<agx.Uuid, agxCollide.Geometry> m_geometries       = new Dictionary<agx.Uuid, agxCollide.Geometry>();
    private Dictionary<agx.Uuid, agx.Constraint>      m_constraints      = new Dictionary<agx.Uuid, agx.Constraint>();
    private Dictionary<agx.Uuid, agx.Material>        m_materials        = new Dictionary<agx.Uuid, agx.Material>();
    private Dictionary<agx.Uuid, agx.ContactMaterial> m_contactMaterials = new Dictionary<agx.Uuid, agx.ContactMaterial>();

    private List<Node> m_roots = new List<Node>();
    private Node m_constraintRoot = new Node();
    private Node m_materialRoot = new Node();
    private Node m_contactMaterialRoot = new Node();
  }
}
