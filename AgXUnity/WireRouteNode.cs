using UnityEngine;
using AgXUnity;
using AgXUnity.Utils;
using System;

namespace AgXUnity
{
  /// <summary>
  /// Representation of nodes, used while routing.
  /// </summary>
  public class WireRouteNode : ScriptAsset
  {
    /// <summary>
    /// Construct a route node given type, parent game object, local position to parent and
    /// local rotation to parent.
    /// </summary>
    /// <param name="type">Node type.</param>
    /// <param name="parent">Parent game object - world if null.</param>
    /// <param name="localPosition">Position in parent frame. If parent is null this is the position in world frame.</param>
    /// <param name="localRotation">Rotation in parent frame. If parent is null this is the rotation in world frame.</param>
    /// <returns>Wire route node instance.</returns>
    public static WireRouteNode Create( Wire.NodeType type = Wire.NodeType.BodyFixedNode, GameObject parent = null, Vector3 localPosition = default( Vector3 ), Quaternion localRotation = default( Quaternion ) )
    {
      WireRouteNode node = Create<WireRouteNode>();

      if ( object.Equals( localRotation, default( Quaternion ) ) )
        localRotation = Quaternion.identity;

      node.Frame.SetParent( parent );
      node.Frame.LocalPosition = localPosition;
      node.Frame.LocalRotation = localRotation;

      node.Type = type;

      return node;
    }

    public agxWire.Node Native { get; private set; }

    /// <summary>
    /// Type of this node. Paired with property Type.
    /// </summary>
    [SerializeField]
    private Wire.NodeType m_type = Wire.NodeType.BodyFixedNode;

    /// <summary>
    /// Type of this node.
    /// </summary>
    public Wire.NodeType Type
    {
      get { return m_type; }
      set
      {
        m_type = value;
        OnNodeType();
      }
    }

    /// <summary>
    /// Frame of this node. Paired with property Frame.
    /// </summary>
    [SerializeField]
    private Frame m_frame = null;

    /// <summary>
    /// Frame of this node. Use this object to position this
    /// node and/or change parent object.
    /// </summary>
    public Frame Frame { get { return m_frame; } }

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
        OnNodeType();
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

    private WireRouteNode()
    {
    }

    protected override void Construct()
    {
      m_frame = Create<Frame>();
      m_type  = Wire.NodeType.BodyFixedNode;
    }

    /// <summary>
    /// Creates native instance given current properties.
    /// </summary>
    /// <returns>Native instance of this node.</returns>
    protected override bool Initialize()
    {
      RigidBody rb        = null;
      Collide.Shape shape = null;
      if ( Frame.Parent != null ) {
        rb    = Frame.Parent.GetInitializedComponentInParent<RigidBody>();
        shape = Frame.Parent.GetInitializedComponentInParent<Collide.Shape>();
      }

      // We don't know if the parent is the rigid body.
      // It could be a mesh, or some other object.
      // Also - use world position if Type == FreeNode.
      agx.Vec3 point = rb != null && Type != Wire.NodeType.FreeNode ?
                          Frame.CalculateLocalPosition( rb.gameObject ).ToHandedVec3() :
                          Frame.Position.ToHandedVec3();

      agx.RigidBody nativeRb = rb != null ? rb.Native : null;
      if ( Type == Wire.NodeType.BodyFixedNode )
        Native = new agxWire.BodyFixedNode( nativeRb, point );
      // Create a free node if type is contact and shape == null.
      else if ( Type == Wire.NodeType.FreeNode || ( Type == Wire.NodeType.ContactNode && shape == null ) )
        Native = new agxWire.FreeNode( point );
      else if ( Type == Wire.NodeType.ConnectingNode )
        Native = new agxWire.ConnectingNode( nativeRb, point, double.PositiveInfinity );
      else if ( Type == Wire.NodeType.EyeNode )
        Native = new agxWire.EyeNode( nativeRb, point );
      else if ( Type == Wire.NodeType.ContactNode )
        Native = new agxWire.ContactNode( shape.NativeGeometry, Frame.CalculateLocalPosition( shape.gameObject ).ToHandedVec3() );
      else if ( Type == Wire.NodeType.WinchNode ) {
        if ( m_winch == null )
          throw new AgXUnity.Exception( "No reference to a wire winch component in the winch node." );

        m_winch.GetInitialized<WireWinch>();

        Native = m_winch.Native != null ? m_winch.Native.getStopNode() : null;
      }

      return Native != null;
    }

    public override void Destroy()
    {
      Native = null;
    }

    private object m_editorData = null;
    public T GetEditorData<T>() where T : class
    {
      return m_editorData as T;
    }

    public void SetEditorData( object editorData ) { m_editorData = editorData; }

    public bool HasEditorData { get { return m_editorData != null; } }

    /// <summary>
    /// When the user is changing node type, e.g., between fixed and winch,
    /// we receive a callback handling winch dependencies.
    /// </summary>
    private void OnNodeType()
    {
      if ( m_winch != null ) {
        if ( Wire == null || Type != Wire.NodeType.WinchNode )
          ScriptAsset.DestroyImmediate( m_winch );
      }
      else if ( Wire != null && Type == Wire.NodeType.WinchNode ) {
        m_winch = WireWinch.Create<WireWinch>();
        m_winch.Wire = Wire;
      }
    }
  }
}
