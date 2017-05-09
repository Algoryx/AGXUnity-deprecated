using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity
{
  /// <summary>
  /// Wire route object containing nodes that initializes a wire.
  /// This object is an IEnumerable, add "using System.Linq" to
  /// get a wide range of "features" such as ToArray().
  /// </summary>
  /// <example>
  /// using System.Linq;
  /// ...
  /// WireRoute route = wire.Route;
  /// var freeNodes = from node in route select node.Type == Wire.NodeType.FreeNode;
  /// Wire.RouteNode myNode = route.FirstOrDefault( node => node.Frame == thisFrame );
  /// </example>
  [Serializable]
  [HideInInspector]
  public class WireRoute : Route<WireRouteNode>
  {
    /// <summary>
    /// Checks validity of current route.
    /// </summary>
    /// <returns>Validated wire route.</returns>
    public override ValidatedRoute GetValidated()
    {
      ValidatedRoute validatedRoute = new ValidatedRoute();
      // Less than thee nodes is always valid from the nodes point of view.
      var nodes = this.ToArray();
      if ( NumNodes < 3 ) {
        for ( int i = 0; i < NumNodes; ++i )
          validatedRoute.Nodes.Add( new ValidatedNode() { Node = nodes[ i ], Valid = true } );
      }
      // More than two nodes. Intermediate nodes may not be body fixed, connecting or winch.
      else {
        validatedRoute.Nodes.Add( new ValidatedNode() { Node = nodes[ 0 ], Valid = true } );
        for ( int i = 1; i < NumNodes - 1; ++i ) {
          WireRouteNode node = nodes[ i ];
          string errorString = node.Type == Wire.NodeType.BodyFixedNode ||
                               node.Type == Wire.NodeType.ConnectingNode ||
                               node.Type == Wire.NodeType.WinchNode ?
                                 node.Type.ToString().SplitCamelCase() + " can only be at the begin or at the end of a wire." :
                               string.Empty;
          validatedRoute.Nodes.Add( new ValidatedNode() { Node = node, Valid = ( errorString == string.Empty ), ErrorString = errorString } );
        }
        validatedRoute.Nodes.Add( new ValidatedNode() { Node = nodes[ NumNodes - 1 ], Valid = true } );
      }

      if ( NumNodes < 2 ) {
        validatedRoute.Valid = false;
        validatedRoute.ErrorString = "Route has to contain at least two or more nodes.";
      }
      else {
        bool nodesValid = true;
        foreach ( var validatedNode in validatedRoute.Nodes )
          nodesValid &= validatedNode.Valid;
        validatedRoute.Valid = nodesValid;
        validatedRoute.ErrorString = "One or more nodes are wrong.";
      }

      return validatedRoute;
    }

    /// <summary>
    /// Wire this route belongs to.
    /// </summary>
    private Wire m_wire = null;

    /// <summary>
    /// Get or set the wire this route belongs to.
    /// </summary>
    public Wire Wire
    {
      get
      {
        if ( m_wire == null ) {
          m_wire = GetComponent<Wire>();
          foreach ( var node in this )
            node.Wire = m_wire;
        }
        return m_wire;
      }
    }

    /// <summary>
    /// Add node to this route given type, parent, local position and local rotation.
    /// </summary>
    /// <param name="type">Node type.</param>
    /// <param name="parent">Node parent object.</param>
    /// <param name="localPosition">Local position relative parent.</param>
    /// <param name="localRotation">Local rotation relative parent.</param>
    /// <returns>Added route node.</returns>
    public WireRouteNode Add( Wire.NodeType type,
                              GameObject parent = null,
                              Vector3 localPosition = default( Vector3 ),
                              Quaternion localRotation = default( Quaternion ) )
    {
      var node = WireRouteNode.Create( type, parent, localPosition, localRotation );
      if ( !Add( node ) )
        return null;

      return node;
    }

    private WireRoute()
    {
      OnNodeAdded   += this.OnAddedToList;
      OnNodeRemoved += this.OnRemovedFromList;
    }

    private void OnAddedToList( WireRouteNode node, int index )
    {
      node.Wire = Wire;
    }

    private void OnRemovedFromList( WireRouteNode node, int index )
    {
      node.Wire = null;
    }
  }
}
