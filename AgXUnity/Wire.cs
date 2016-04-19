using AgXUnity.Utils;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Wire object.
  /// </summary>
  [AddComponentMenu( "" )]
  [RequireComponent( typeof( Rendering.WireRenderer ) )]
  [GenerateCustomEditor]
  public class Wire : ScriptComponent
  {
    /// <summary>
    /// Route nodes used when routing the wires.
    /// </summary>
    [System.Serializable]
    public class RouteNode
    {
      public enum NodeType
      {
        BodyFixedNode,
        FreeNode,
        ConnectingNode,
        EyeNode,
        ContactNode,
        WinchNode
      }

      /// <summary>
      /// Local position of the node.
      /// </summary>
      [SerializeField]
      private Vector3 m_position = new Vector3();

      /// <summary>
      /// Get or set local position of this node.
      /// </summary>
      public Vector3 LocalPosition
      {
        get { return m_position; }
        set
        {
          m_position = value;
          if ( m_winch != null )
            m_winch.LocalPosition = m_position;
        }
      }

      /// <summary>
      /// If this route node is a winch, this field is set.
      /// </summary>
      [SerializeField]
      private WireWinch m_winch = null;

      /// <summary>
      /// Get winch if assigned from the route.
      /// </summary>
      public WireWinch Winch { get { return m_winch; } }

      /// <summary>
      /// Reference back to the wire.
      /// </summary>
      [SerializeField]
      private Wire m_wire = null;

      /// <summary>
      /// Get or set wire of this route node.
      /// </summary>
      public Wire Wire
      {
        get { return m_wire; }
        set
        {
          m_wire = value;
          if ( m_wire != null )
            OnNodeType( Type );
        }
      }

      /// <summary>
      /// Get or set world position of this node.
      /// </summary>
      public Vector3 WorldPosition
      {
        get
        {
          return Parent != null ? Parent.transform.TransformPoint( LocalPosition ) : LocalPosition;
        }
        set
        {
          LocalPosition = Parent != null ? Parent.transform.InverseTransformPoint( value ) : value;
        }
      }

      /// <summary>
      /// Local rotation of this node. Mainly used for winches.
      /// </summary>
      [SerializeField]
      private Quaternion m_localRotation = Quaternion.identity;

      /// <summary>
      /// Get or set local rotation of this node. Mainly used for winches.
      /// </summary>
      public Quaternion LocalRotation
      {
        get { return m_localRotation; }
        set
        {
          m_localRotation = value;
          if ( m_winch != null )
            m_winch.LocalDirection = m_localRotation * Vector3.forward;
        }
      }

      /// <summary>
      /// Node type of this route node. Default free.
      /// </summary>
      [SerializeField]
      private NodeType m_nodeType = NodeType.FreeNode;

      /// <summary>
      /// Get or set node type of this route node. Default node type: Free.
      /// </summary>
      public NodeType Type
      {
        get { return m_nodeType; }
        set
        {
          m_nodeType = value;
          OnNodeType( m_nodeType );
        }
      }

      /// <summary>
      /// The parent is the object this node is attached to. If the parent
      /// is null, the node is attached in/released from world.
      /// </summary>
      [SerializeField]
      private GameObject m_parent = null;

      /// <summary>
      /// Get or set the parent game object of this route node. When the parent
      /// is moved (e.g., in the editor), the node follows.
      /// </summary>
      public GameObject Parent
      {
        get { return m_parent; }
        set
        {
          m_parent = value;
          if ( m_winch != null )
            m_winch.Parent = m_parent;
        }
      }

      /// <summary>
      /// Create native node given current properties.
      /// </summary>
      /// <returns>Native wire node.</returns>
      public agxWire.Node CreateNode()
      {
        RigidBody rb = null;
        Collide.Shape shape = null;
        if ( Parent != null ) {
          rb = Parent.GetInitializedComponentInParent<RigidBody>();
          shape = Parent.GetInitializedComponentInParent<Collide.Shape>();
        }

        // We don't know if the parent is the rigid body.
        // It could be a mesh, or some other object.
        agx.Vec3 point = rb != null ?
                          rb.transform.InverseTransformPoint( WorldPosition ).ToHandedVec3() :
                          WorldPosition.ToHandedVec3();

        agx.RigidBody nativeRb = rb != null ? rb.Native : null;
        if ( Type == NodeType.BodyFixedNode )
          return new agxWire.BodyFixedNode( nativeRb, point );
        // Create a free node if type is contact and shape == null.
        else if ( Type == NodeType.FreeNode || ( Type == NodeType.ContactNode && shape == null ) )
          return new agxWire.FreeNode( point );
        else if ( Type == NodeType.ConnectingNode )
          return new agxWire.ConnectingNode( nativeRb, point, double.PositiveInfinity );
        else if ( Type == NodeType.EyeNode )
          return new agxWire.EyeNode( nativeRb, point );
        else if ( Type == NodeType.ContactNode )
          return new agxWire.ContactNode( shape.NativeGeometry, shape.transform.InverseTransformPoint( WorldPosition ).ToHandedVec3() );
        else if ( Type == NodeType.WinchNode ) {
          if ( m_winch == null )
            throw new AgXUnity.Exception( "No reference to a wire winch component in the winch node." );

          m_winch.GetInitialized<WireWinch>();
          return m_winch.Native != null ? m_winch.Native.getStopNode() : null;
        }

        return null;
      }

      /// <summary>
      /// When the user is changing node type, e.g., between fixed and winch,
      /// we receive a callback handling winch dependencies.
      /// </summary>
      /// <param name="newType">New node type.</param>
      private void OnNodeType( NodeType newType )
      {
        if ( newType == NodeType.WinchNode && m_winch != null )
          return;

        if ( newType != NodeType.WinchNode && m_winch != null ) {
          GameObject.DestroyImmediate( m_winch );
          return;
        }

        if ( newType == NodeType.WinchNode && m_winch == null && this.Wire != null ) {
          m_winch = this.Wire.gameObject.AddComponent<WireWinch>();
          m_winch.Parent = Parent;
          return;
        }
      }
    }

    /// <summary>
    /// Native instance.
    /// </summary>
    private agxWire.Wire m_wire = null;

    /// <summary>
    /// Get native instance, if initialized.
    /// </summary>
    public agxWire.Wire Native { get { return m_wire; } }

    /// <summary>
    /// Radius of this wire - default 0.015. Paired with property Radius.
    /// </summary>
    [SerializeField]
    private float m_radius = 0.015f;

    /// <summary>
    /// Get or set radius of this wire - default 0.015.
    /// </summary>
    [ClampAboveZeroInInspector]
    public float Radius
    {
      get { return m_radius; }
      set
      {
        m_radius = value;
        if ( m_wire != null )
          m_wire.setRadius( m_radius );
      }
    }

    /// <summary>
    /// Convenience property for diameter of this wire.
    /// </summary>
    [ClampAboveZeroInInspector]
    public float Diameter
    {
      get { return 2.0f * Radius; }
      set { Radius = 0.5f * value; }
    }

    /// <summary>
    /// Resolution of this wire - default 1.5. Paired with property ResolutionPerUnitLength.
    /// </summary>
    [SerializeField]
    private float m_resolutionPerUnitLength = 1.5f;

    /// <summary>
    /// Get or set resolution of this wire. Default 1.5.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public float ResolutionPerUnitLength
    {
      get { return m_resolutionPerUnitLength; }
      set
      {
        m_resolutionPerUnitLength = value;
        if ( m_wire != null )
          m_wire.setResolutionPerUnitLength( m_resolutionPerUnitLength );
      }
    }

    /// <summary>
    /// Linear velocity damping of this wire.
    /// </summary>
    [SerializeField]
    private float m_linearVelocityDamping = 0.0f;

    /// <summary>
    /// Get or set linear velocity damping of this wire. Default 0.0.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public float LinearVelocityDamping
    {
      get { return m_linearVelocityDamping; }
      set
      {
        m_linearVelocityDamping = value;
        if ( m_wire != null )
          m_wire.setLinearVelocityDamping( m_linearVelocityDamping );
      }
    }

    /// <summary>
    /// Internal. Scale constant of this wire - default 0.35. Paired with
    /// property ScaleConstant.
    /// </summary>
    [SerializeField]
    private float m_scaleConstant = 0.35f;

    /// <summary>
    /// Internal. Get or set scale constant of this wire. Default 0.35.
    /// </summary>
    [ClampAboveZeroInInspector]
    public float ScaleConstant
    {
      get { return m_scaleConstant; }
      set
      {
        m_scaleConstant = value;
        if ( m_wire != null )
          m_wire.getParameterController().setScaleConstant( m_scaleConstant );
      }
    }

    /// <summary>
    /// Shape material of this wire. Default null.
    /// </summary>
    [SerializeField]
    private ShapeMaterial m_material = null;

    /// <summary>
    /// Get or set shape material of this wire. Default null.
    /// </summary>
    public ShapeMaterial Material
    {
      get { return m_material; }
      set
      {
        m_material = value;
        if ( Native != null && m_material != null && m_material.Native != null )
          Native.setMaterial( m_material.Native );
      }
    }

    /// <summary>
    /// Route to initialize this wire.
    /// </summary>
    [SerializeField]
    private WireRoute m_route = new WireRoute();

    /// <summary>
    /// Get route to initialize this wire.
    /// </summary>
    [HideInInspector]
    public WireRoute Route
    {
      get { return m_route; }
      set
      {
        m_route = value;
        if ( m_route != null )
          m_route.Wire = this;
      }
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public Wire()
    {
      m_route.Wire = this;
    }

    protected override bool Initialize()
    {
      if ( m_route == null )
        return false;

      try {
        m_wire = new agxWire.Wire( Radius, ResolutionPerUnitLength );
        Material = m_material != null ? m_material.GetInitialized<ShapeMaterial>() : null;
        int nodeCounter = 0;
        foreach ( RouteNode routeNode in m_route.Nodes ) {
          agxWire.Node node = routeNode.CreateNode();

          bool success = true;
          if ( node.getType() == agxWire.Node.Type.CONNECTING ) {
            // This is the first node, CM-node goes first.
            if ( nodeCounter == 0 ) {
              success = success && m_wire.add( node.getAsConnecting().getCmNode() );
              success = success && m_wire.add( node );
            }
            // This has to be the last node, CM-node goes last.
            else {
              success = success && m_wire.add( node );
              success = success && m_wire.add( node.getAsConnecting().getCmNode() );
            }
          }
          else if ( routeNode.Type == RouteNode.NodeType.WinchNode ) {
            if ( node == null )
              throw new AgXUnity.Exception( "Unable to initialize wire winch." );

            success = success && m_wire.add( routeNode.Winch.Native );
          }
          else
            success = success && m_wire.add( node );

          if ( !success )
            throw new AgXUnity.Exception( "Unable to add node " + nodeCounter + ": " + routeNode.Type );

          ++nodeCounter;
        }

        GetSimulation().add( m_wire );
      }
      catch ( System.Exception e ) {
        Debug.LogException( e, this );
        return false;
      }

      return m_wire.initialized();
    }

    protected override void OnDestroy()
    {
      if ( GetSimulation() != null )
        GetSimulation().remove( Native );

      m_wire = null;

      base.OnDestroy();
    }
  }
}
