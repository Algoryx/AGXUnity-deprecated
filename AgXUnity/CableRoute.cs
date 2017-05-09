using System;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Cable route object containing nodes that initializes a cable.
  /// This object is an IEnumerable, add "using System.Linq" to
  /// get a wide range of "features" such as ToArray().
  /// </summary>
  [HideInInspector]
  public class CableRoute : Route<CableRouteNode>
  {
    /// <summary>
    /// Add node to this route given type, parent, local position and local rotation.
    /// </summary>
    /// <param name="nodeType">Node type.</param>
    /// <param name="parent">Node parent object.</param>
    /// <param name="localPosition">Local position relative parent.</param>
    /// <param name="localRotation">Local rotation relative parent.</param>
    /// <returns></returns>
    public CableRouteNode Add( Cable.NodeType nodeType,
                               GameObject parent = null,
                               Vector3 localPosition = default( Vector3 ),
                               Quaternion localRotation = default( Quaternion ) )
    {
      var node = CableRouteNode.Create( nodeType, parent, localPosition, localRotation );
      if ( !Add( node ) )
        return null;

      return node;
    }
  }
}
