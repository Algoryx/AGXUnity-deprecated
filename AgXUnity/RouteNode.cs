using System;
using UnityEngine;

namespace AgXUnity
{
  [Serializable]
  public abstract class RouteNode : IFrame
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
      where T : RouteNode, new()
    {
      T node = new T();

      if ( object.Equals( localRotation, default( Quaternion ) ) )
        localRotation = Quaternion.identity;

      node.SetParent( parent );
      node.LocalPosition = localPosition;
      node.LocalRotation = localRotation;

      return node;
    }
  }
}
