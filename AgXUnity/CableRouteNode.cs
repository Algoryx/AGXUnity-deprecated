using System;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity
{
  public class CableRouteNode : ScriptAsset
  {
    /// <summary>
    /// Construct a route node given type, parent game object, local position to parent and
    /// local rotation to parent.
    /// </summary>
    /// <param name="type">Node type.</param>
    /// <param name="parent">Parent game object - world if null.</param>
    /// <param name="localPosition">Position in parent frame. If parent is null this is the position in world frame.</param>
    /// <param name="localRotation">Rotation in parent frame. If parent is null this is the rotation in world frame.</param>
    /// <returns>Cable route node instance.</returns>
    public static CableRouteNode Create( Cable.NodeType nodeType = Cable.NodeType.BodyFixedNode,
                                         GameObject parent = null,
                                         Vector3 localPosition = default( Vector3 ),
                                         Quaternion localRotation = default( Quaternion ) )
    {
      CableRouteNode node = Create<CableRouteNode>();

      if ( object.Equals( localRotation, default( Quaternion ) ) )
        localRotation = Quaternion.identity;

      node.Frame.SetParent( parent );
      node.Frame.LocalPosition = localPosition;
      node.Frame.LocalRotation = localRotation;

      node.Type = nodeType;

      return node;
    }

    /// <summary>
    /// Native instance of this node - present after Initialize has been called.
    /// </summary>
    public agxCable.Node Native { get; private set; }

    /// <summary>
    /// Type of this node. Paired with property Type.
    /// </summary>
    [SerializeField]
    private Cable.NodeType m_type = Cable.NodeType.BodyFixedNode;

    /// <summary>
    /// Type of this node.
    /// </summary>
    public Cable.NodeType Type
    {
      get { return m_type; }
      set { m_type = value; }
    }

    /// <summary>
    /// Frame of this node holding position, rotation and parenting.
    /// The first rigid body (if any) will be the body added to the node.
    /// </summary>
    [SerializeField]
    private Frame m_frame = null;

    /// <summary>
    /// Frame of this node holding position, rotation and parenting.
    /// The first rigid body (if any) will be the body added to the node.
    /// </summary>
    public Frame Frame { get { return m_frame; } }

    public override void Destroy()
    {
      Native = null;
    }

    protected override void Construct()
    {
      m_frame = Create<Frame>();
    }

    protected override bool Initialize()
    {
      RigidBody rb = Frame.Parent != null ? Frame.Parent.GetInitializedComponentInParent<RigidBody>() : null;

      agx.Vec3 position = rb != null && Type == Cable.NodeType.BodyFixedNode ?
                            Frame.CalculateLocalPosition( rb.gameObject ).ToHandedVec3() :
                            Frame.Position.ToHandedVec3();

      agx.Quat rotation = rb != null && Type == Cable.NodeType.BodyFixedNode ?
                            Frame.CalculateLocalRotation( rb.gameObject ).ToHandedQuat() :
                            Frame.Rotation.ToHandedQuat();

      if ( Type == Cable.NodeType.BodyFixedNode )
        Native = new agxCable.BodyFixedNode( rb.Native, new agx.AffineMatrix4x4( rotation, position ) );
      else if ( Type == Cable.NodeType.FreeNode )
        Native = new agxCable.FreeNode( position );
      else
        return false;

      return true;
    }
  }
}
