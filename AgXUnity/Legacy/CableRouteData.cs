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

    public bool Restore()
    {
      var route = GetComponent<CableRoute>();
      if ( route == null )
        return false;

      foreach ( var nodeData in m_data ) {
        var node = route.Add( nodeData.NodeType, nodeData.Parent, nodeData.LocalPosition, nodeData.LocalRotation );
        if ( node == null ) {
          Debug.LogWarning( "Unable to add node of type " + nodeData.NodeType + " to cable route.", route );
          continue;
        }
      }

      return true;
    }
  }
}
