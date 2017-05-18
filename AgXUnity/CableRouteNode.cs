using System;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity
{
  [Serializable]
  public class CableRouteNode : RouteNode
  {
    /// <summary>
    /// Construct a route node given parent game object, local position to parent and
    /// local rotation to parent.
    /// </summary>
    /// <param name="parent">Parent game object - world if null.</param>
    /// <param name="localPosition">Position in parent frame. If parent is null this is the position in world frame.</param>
    /// <param name="localRotation">Rotation in parent frame. If parent is null this is the rotation in world frame.</param>
    /// <returns>Route node instance.</returns>
    public static CableRouteNode Create( Cable.NodeType nodeType,
                                         GameObject parent = null,
                                         Vector3 localPosition = default( Vector3 ),
                                         Quaternion localRotation = default( Quaternion ) )
    {
      var node = Create<CableRouteNode>( parent, localPosition, localRotation );
      node.Type = nodeType;

      return node;
    }

    /// <summary>
    /// Native instance of this node - present after Initialize has been called.
    /// </summary>
    public agxCable.RoutingNode Native { get; private set; }

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

    public override void OnDestroy()
    {
      Native = null;

      base.OnDestroy();
    }

    protected override bool Initialize()
    {
      if ( Native != null )
        return true;

      RigidBody rb = Parent != null ? Parent.GetInitializedComponentInParent<RigidBody>() : null;

      agx.Vec3 position = rb != null && Type == Cable.NodeType.BodyFixedNode ?
                            CalculateLocalPosition( rb.gameObject ).ToHandedVec3() :
                            Position.ToHandedVec3();

      agx.Quat rotation = rb != null && Type == Cable.NodeType.BodyFixedNode ?
                            CalculateLocalRotation( rb.gameObject ).ToHandedQuat() :
                            Rotation.ToHandedQuat();

      if ( Type == Cable.NodeType.BodyFixedNode )
        Native = new agxCable.BodyFixedNode( rb != null ? rb.Native : null, new agx.AffineMatrix4x4( rotation, position ) );
      else if ( Type == Cable.NodeType.FreeNode ) {
        Native = new agxCable.FreeNode( position );
        Native.getRigidBody().setRotation( agx.Quat.rotate( agx.Vec3.Z_AXIS(), ( Rotation * Vector3.forward ).ToHandedVec3() ) );
      }
      else
        return false;

      return true;
    }
  }
}
