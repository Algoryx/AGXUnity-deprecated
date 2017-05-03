using System;
using UnityEngine;

namespace AgXUnity
{
  public abstract class RouteNode : ScriptAsset
  {
    /// <summary>
    /// Construct a route node given parent game object, local position to parent and
    /// local rotation to parent.
    /// </summary>
    /// <param name="parent">Parent game object - world if null.</param>
    /// <param name="localPosition">Position in parent frame. If parent is null this is the position in world frame.</param>
    /// <param name="localRotation">Rotation in parent frame. If parent is null this is the rotation in world frame.</param>
    /// <returns>Route node instance.</returns>
    public static T Create<T>( GameObject parent = null,
                                       Vector3 localPosition = default( Vector3 ),
                                       Quaternion localRotation = default( Quaternion ) )
      where T : RouteNode
    {
      T node = ScriptAsset.Create<T>();

      if ( object.Equals( localRotation, default( Quaternion ) ) )
        localRotation = Quaternion.identity;

      node.Frame.SetParent( parent );
      node.Frame.LocalPosition = localPosition;
      node.Frame.LocalRotation = localRotation;

      return node;
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

    protected override void Construct()
    {
      //m_frame = ScriptAsset.Create<Frame>();
    }
  }
}
