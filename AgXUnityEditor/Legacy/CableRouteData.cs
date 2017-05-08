using System;
using System.Collections.Generic;
using UnityEngine;
using AgXUnity;

namespace AgXUnityEditor.Legacy
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

  public class CableRouteData : ScriptableObject
  {
    [SerializeField]
    private List<CableRouteNodeData> m_data = new List<CableRouteNodeData>();
  }
}
