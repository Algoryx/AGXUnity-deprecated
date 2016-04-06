using System.Collections.Generic;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Wire route object containing nodes that initializes a wire.
  /// </summary>
  [System.Serializable]
  public class WireRoute
  {
    /// <summary>
    /// List of route nodes.
    /// </summary>
    [SerializeField]
    private List<Wire.RouteNode> m_nodes = new List<Wire.RouteNode>();

    /// <summary>
    /// Get the current list of rout nodes.
    /// </summary>
    public List<Wire.RouteNode> Nodes { get { return m_nodes; } }

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
    /// <param name="node"></param>
    public void Add( Wire.RouteNode node )
    {
      if ( node == null )
        return;

      node.Wire = Wire;

      m_nodes.Add( node );
    }
  }
}
