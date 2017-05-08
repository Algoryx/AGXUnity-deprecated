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

    public static CableRouteData Create( CableRoute cableRoute )
    {
      return CreateInstance<CableRouteData>().Construct( cableRoute );
    }

    public CableRouteData Construct( CableRoute wireRoute )
    {
      m_data.Clear();

      foreach ( var node in wireRoute ) {
        m_data.Add( new CableRouteNodeData()
        {
          NodeType = node.Type,
          Parent = node.Frame.Parent,
          LocalPosition = node.Frame.LocalPosition,
          LocalRotation = node.Frame.LocalRotation,
        } );
      }

      return this;
    }
  }
}
