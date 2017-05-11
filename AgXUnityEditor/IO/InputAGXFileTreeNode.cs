using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AgXUnityEditor.IO
{
  public class InputAGXFileTreeNode
  {
    public enum NodeType
    {
      Geometry,
      RigidBody,
      Assembly,
      Constraint,
      Material,
      ContactMaterial
    }

    public NodeType Type { get; set; }

    public agx.Uuid Uuid { get; set; }

    public InputAGXFileTreeNode Parent { get; private set; }

    public InputAGXFileTreeNode[] Children { get { return m_children.ToArray(); } }

    public InputAGXFileTreeNode[] References { get { return m_references.ToArray(); } }

    public GameObject GameObject { get; set; }

    public void AddChild( InputAGXFileTreeNode child )
    {
      if ( child == null ) {
        Debug.LogWarning( "Trying to add null child to parent: " + Type + ", (UUID: " + Uuid.ToString() + ")" );
        return;
      }

      if ( child.Parent != null ) {
        Debug.LogError( "Node already have a parent." );
        return;
      }

      child.Parent = this;
      m_children.Add( child );
    }

    public void AddReference( InputAGXFileTreeNode reference )
    {
      if ( reference == null )
        return;

      if ( !m_references.Contains( reference ) )
        m_references.Add( reference );

      reference.m_references.Add( this );
    }

    private List<InputAGXFileTreeNode> m_children   = new List<InputAGXFileTreeNode>();
    private List<InputAGXFileTreeNode> m_references = new List<InputAGXFileTreeNode>();
  }
}
