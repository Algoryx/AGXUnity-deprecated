using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using AgXUnity.Collide;
using AgXUnity.Utils;

namespace AgXUnity
{
  [GenerateCustomEditor]
  public abstract class DebugRenderData : ScriptComponent
  {
    [SerializeField]
    private GameObject m_node = null;
    public GameObject Node
    {
      get { return m_node; }
      set
      {
        m_node = value;
      }
    }

    [SerializeField]
    private bool m_visible = true;
    public bool Visible
    {
      get { return m_visible; }
      set
      {
        m_visible = value;
        if ( m_node != null )
          m_node.SetActive( m_visible );
      }
    }

    public abstract string GetTypeName();
    public abstract void Synchronize();

    [HideInInspector]
    public string PrefabName { get { return GetPrefabName( GetTypeName() ); } }

    public static string GetPrefabName( string typeName )
    {
      return "Debug." + typeName + "Renderer";
    }
  }
}
