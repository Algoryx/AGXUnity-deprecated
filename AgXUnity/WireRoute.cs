using System;
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
  [System.Serializable]
  public class WireRoute : IEnumerable<WireRouteNode>
  {
    public class ValidatedNode
    {
      public WireRouteNode Node = null;
      public bool Valid         = true;
      public string ErrorString = string.Empty;
    }

    public class ValidatedRoute : IEnumerable<ValidatedNode>
    {
      public bool Valid                = true;
      public string ErrorString        = string.Empty;
      public List<ValidatedNode> Nodes = new List<ValidatedNode>();

      public IEnumerator<ValidatedNode> GetEnumerator()
      {
        return Nodes.GetEnumerator();
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
      {
        return GetEnumerator();
      }
    }

    public ValidatedRoute GetValidated()
    {
      ValidatedRoute validatedRoute = new ValidatedRoute();
      // Less than thee nodes is always valid from the nodes point of view.
      if ( m_nodes.Count < 3 ) {
        for ( int i = 0; i < NumNodes; ++i )
          validatedRoute.Nodes.Add( new ValidatedNode() { Node = m_nodes[ i ], Valid = true } );
      }
      // More than two nodes. Intermediate nodes may not be body fixed, connecting or winch.
      else {
        validatedRoute.Nodes.Add( new ValidatedNode() { Node = m_nodes[ 0 ], Valid = true } );
        for ( int i = 1; i < NumNodes - 1; ++i ) {
          WireRouteNode node = m_nodes[ i ];
          string errorString = node.Type == Wire.NodeType.BodyFixedNode ||
                               node.Type == Wire.NodeType.ConnectingNode ||
                               node.Type == Wire.NodeType.WinchNode ?
                                 node.Type.ToString().SplitCamelCase() + " can only be at the begin or at the end of a wire." :
                               string.Empty;
          validatedRoute.Nodes.Add( new ValidatedNode() { Node = node, Valid = ( errorString == string.Empty ), ErrorString = errorString } );
        }
        validatedRoute.Nodes.Add( new ValidatedNode() { Node = m_nodes[ NumNodes - 1 ], Valid = true } );
      }

      if ( m_nodes.Count < 2 ) {
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
    /// List of route nodes.
    /// </summary>
    [SerializeField]
    private List<WireRouteNode> m_nodes = new List<WireRouteNode>();

    /// <summary>
    /// Number of nodes in route.
    /// </summary>
    public int NumNodes { get { return m_nodes.Count; } }

    /// <summary>
    /// Wire this route belongs to.
    /// </summary>
    [SerializeField]
    private Wire m_wire = null;

    /// <summary>
    /// Get or set the wire this route belongs to.
    /// </summary>
    public Wire Wire
    {
      get { return m_wire; }
      set
      {
        m_wire = value;
        m_nodes.ForEach( node => node.Wire = m_wire );
      }
    }

    /// <summary>
    /// Add route node to route.
    /// </summary>
    /// <param name="node">Node to add.</param>
    public bool Add( WireRouteNode node )
    {
      if ( node == null || m_nodes.Contains( node ) )
        return false;

      m_nodes.Add( node );

      OnAddedToList( node );

      return true;
    }

    public bool InsertBefore( WireRouteNode nodeToInsert, WireRouteNode beforeThisNode )
    {
      if ( nodeToInsert == null || m_nodes.Contains( nodeToInsert ) )
        return false;

      int index = m_nodes.IndexOf( beforeThisNode );
      if ( index < 0 )
        return false;

      m_nodes.Insert( index, nodeToInsert );

      OnAddedToList( nodeToInsert );

      return true;
    }

    /// <summary>
    /// Insert <paramref name="nodeToInsert"/> into this route, after <paramref name="afterThisNode"/>.
    /// </summary>
    /// <param name="nodeToInsert">Node to insert.</param>
    /// <param name="afterThisNode">Insert <paramref name="nodeToInsert"/> after this node.</param>
    /// <returns>True if <paramref name="afterThisNode"/> is valid and in list and <paramref name="nodeToInsert"/> is successfully inserted.</returns>
    public bool InsertAfter( WireRouteNode nodeToInsert, WireRouteNode afterThisNode )
    {
      if ( nodeToInsert == null || m_nodes.Contains( nodeToInsert ) )
        return false;

      int index = m_nodes.IndexOf( afterThisNode );
      if ( index < 0 )
        return false;

      m_nodes.Insert( index + 1, nodeToInsert );

      OnAddedToList( nodeToInsert );

      return true;
    }

    /// <summary>
    /// Remove node from route.
    /// </summary>
    /// <param name="node">Node to remove.</param>
    public bool Remove( WireRouteNode node )
    {
      if ( node == null )
        return false;

      int index = m_nodes.IndexOf( node );
      if ( index < 0 )
        return false;

      m_nodes.RemoveAt( index );
      OnRemovedFromList( node, index );

      return true;
    }

    /// <summary>
    /// Callback fired when a node has been added/inserted into this route.
    /// </summary>
    public Action<WireRouteNode> OnNodeAdded = delegate { };

    /// <summary>
    /// Callback fired when a node has been removed from this route.
    /// Signature: OnNodeRemoved( WireRouteNode removedNode, int indexOfRemovedNode ).
    /// </summary>
    public Action<WireRouteNode, int> OnNodeRemoved = delegate { };

    public IEnumerator<WireRouteNode> GetEnumerator()
    {
      return m_nodes.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    private void OnAddedToList( WireRouteNode node )
    {
      node.Wire = Wire;
      OnNodeAdded( node );
    }

    private void OnRemovedFromList( WireRouteNode node, int index )
    {
      OnNodeRemoved( node, index );
      node.Wire = null;
    }
  }
}
