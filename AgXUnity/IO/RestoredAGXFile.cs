using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AgXUnity.IO
{
  [Serializable]
  public class GroupPair
  {
    public string First = string.Empty;
    public string Second = string.Empty;
  }

  public class RestoredAGXFile : ScriptComponent
  {
    [SerializeField]
    private List<GroupPair> m_disabledGroups = new List<GroupPair>();

    [HideInInspector]
    public GroupPair[] DisabledGroups { get { return m_disabledGroups.ToArray(); } }

    public void AddDisabledPair( string group1, string group2 )
    {
      m_disabledGroups.Add( new GroupPair() { First = group1, Second = group2 } );
    }
  }
}
