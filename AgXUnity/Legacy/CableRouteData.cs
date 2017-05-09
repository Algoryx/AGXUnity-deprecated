using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgXUnity.Legacy
{
  [Serializable]
  public class CableRouteNodeData
  {
    [SerializeField]
    public Cable.NodeType NodeType = Cable.NodeType.BodyFixedNode;
    [SerializeField]
    public GameObject Parent = null;
    [SerializeField]
    public Vector3 LocalPosition = Vector3.zero;
    [SerializeField]
    public Quaternion LocalRotation = Quaternion.identity;
  }

  public class CableRouteData : ScriptComponent
  {
    [SerializeField]
    private List<CableRouteNodeData> m_data = new List<CableRouteNodeData>();

    public void Save()
    {
      hideFlags = HideFlags.HideInInspector;

      m_data.Clear();

      var cable = GetComponent<Cable>();
      if ( cable == null )
        return;

      foreach ( var node in cable.Route ) {
        m_data.Add( new CableRouteNodeData()
        {
          NodeType = node.Type,
          Parent = node.Frame.Parent,
          LocalPosition = node.Frame.LocalPosition,
          LocalRotation = node.Frame.LocalRotation,
        } );
      }
    }
  }
}
