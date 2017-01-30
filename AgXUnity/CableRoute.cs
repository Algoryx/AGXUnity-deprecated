using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// <summary>
  /// Wire route object containing nodes that initializes a wire.
  /// This object is an IEnumerable, add "using System.Linq" to
  /// get a wide range of "features" such as ToArray().
  /// </summary>
  [Serializable]
  public class CableRoute : IEnumerable<CableRouteNode>
  {
    /// <summary>
    /// Route node list.
    /// </summary>
    [SerializeField]
    private List<CableRouteNode> m_nodes = new List<CableRouteNode>();

    /// <summary>
    /// Number of nodes in route.
    /// </summary>
    public int NumNodes { get { return m_nodes.Count; } }

    /// <summary>
    /// Add new node to route.
    /// </summary>
    /// <param name="node">Node to add.</param>
    /// <returns>True if the node is added, false if null or already present.</returns>
    public bool Add( CableRouteNode node )
    {
      if ( node == null || m_nodes.Contains( node ) )
        return false;

      return TryInsertAtIndex( m_nodes.Count, node );
    }

    /// <summary>
    /// Insert node before another given node, already present in route.
    /// </summary>
    /// <param name="nodeToInsert">Node to insert.</param>
    /// <param name="beforeThisNode">Insert <paramref name="nodeToInsert"/> before this node.</param>
    /// <returns>True if inserted, false if null or already present.</returns>
    public bool InsertBefore( CableRouteNode nodeToInsert, CableRouteNode beforeThisNode )
    {
      if ( nodeToInsert == null || beforeThisNode == null )
        return false;

      return TryInsertAtIndex( m_nodes.IndexOf( beforeThisNode ), nodeToInsert );
    }

    /// <summary>
    /// Insert node after another given node, already present in route.
    /// </summary>
    /// <param name="nodeToInsert">Node to insert.</param>
    /// <param name="afterThisNode">Insert <paramref name="nodeToInsert"/> before this node.</param>
    /// <returns>True if inserted, false if null or already present.</returns>
    public bool InsertAfter( CableRouteNode nodeToInsert, CableRouteNode afterThisNode )
    {
      if ( nodeToInsert == null || afterThisNode == null || !m_nodes.Contains( afterThisNode ) )
        return false;

      return TryInsertAtIndex( m_nodes.IndexOf( afterThisNode ) + 1, nodeToInsert );
    }

    private bool TryInsertAtIndex( int index, CableRouteNode node )
    {
      // According to List documentation having index == m_nodes.Count is
      // valid and the new node will be added to the list.
      if ( index < 0 || index > m_nodes.Count || m_nodes.Contains( node ) )
        return false;

      m_nodes.Insert( index, node );

      return true;
    }

    public IEnumerator<CableRouteNode> GetEnumerator()
    {
      return m_nodes.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}
