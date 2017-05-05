using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgXUnity
{
  public class Route<T> : ScriptComponent, IEnumerable<T>
    where T : RouteNode
  {
    /// <summary>
    /// Route node list.
    /// </summary>
    [SerializeField]
    private List<T> m_nodes = new List<T>();

    /// <summary>
    /// Number of nodes in route.
    /// </summary>
    public int NumNodes { get { return m_nodes.Count; } }

    /// <summary>
    /// Finds index of the node in the list.
    /// </summary>
    /// <param name="node">Node to find index of.</param>
    /// <returns>Index of the node in route list - -1 if not found.</returns>
    public int IndexOf( T node )
    {
      return m_nodes.IndexOf( node );
    }

    /// <summary>
    /// Add new node to route.
    /// </summary>
    /// <param name="node">Node to add.</param>
    /// <returns>True if the node is added, false if null or already present.</returns>
    public bool Add( T node )
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
    public bool InsertBefore( T nodeToInsert, T beforeThisNode )
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
    public bool InsertAfter( T nodeToInsert, T afterThisNode )
    {
      if ( nodeToInsert == null || afterThisNode == null || !m_nodes.Contains( afterThisNode ) )
        return false;

      return TryInsertAtIndex( m_nodes.IndexOf( afterThisNode ) + 1, nodeToInsert );
    }

    /// <summary>
    /// Remove node from route.
    /// </summary>
    /// <param name="node">Node to remove.</param>
    /// <returns>True if removed, otherwise false.</returns>
    public bool Remove( T node )
    {
      return m_nodes.Remove( node );
    }

    protected override bool Initialize()
    {
      foreach ( var node in this )
        node.GetInitialized<T>();

      return true;
    }

    protected override void OnDestroy()
    {
      foreach ( var node in this )
        node.OnDestroy();

      base.OnDestroy();
    }

    private bool TryInsertAtIndex( int index, T node )
    {
      // According to List documentation having index == m_nodes.Count is
      // valid and the new node will be added to the list.
      if ( index < 0 || index > m_nodes.Count || m_nodes.Contains( node ) )
        return false;

      m_nodes.Insert( index, node );

      return true;
    }

    public IEnumerator<T> GetEnumerator()
    {
      return m_nodes.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}
