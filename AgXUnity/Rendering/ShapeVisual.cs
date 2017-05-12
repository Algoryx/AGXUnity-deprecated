using System;
using UnityEngine;

namespace AgXUnity.Rendering
{
  [RequireComponent( typeof( MeshRenderer ) )]
  [RequireComponent( typeof( MeshFilter ) )]
  public class ShapeVisual : ScriptComponent
  {
    private MeshRenderer m_renderer = null;
    private MeshFilter m_filter = null;

    public MeshRenderer MeshRenderer
    {
      get
      {
        if ( m_renderer == null )
          m_renderer = GetComponent<MeshRenderer>();
        return m_renderer;
      }
    }

    public MeshFilter MeshFilter
    {
      get
      {
        if ( m_filter == null )
          m_filter = GetComponent<MeshFilter>();
        return m_filter;
      }
    }
  }
}
