using System;
using System.Collections.Generic;
using UnityEngine;
using AgXUnity;

namespace AgXUnityEditor.Legacy
{
  [Serializable]
  public class WireRouteWinchData
  {
    [SerializeField]
    public float Speed = 0.0f;
    [SerializeField]
    public float PulledInLength = 0.0f;
    [SerializeField]
    public RangeReal ForceRange = new RangeReal();
    [SerializeField]
    public RangeReal BrakeForceRange = new RangeReal() { Min = 0.0f, Max = 0.0f };
  }

  [Serializable]
  public class WireRouteNodeData
  {
    [SerializeField]
    public Wire.NodeType NodeType = Wire.NodeType.BodyFixedNode;
    [SerializeField]
    public GameObject Parent = null;
    [SerializeField]
    public Vector3 LocalPosition = Vector3.zero;
    [SerializeField]
    public Quaternion LocalRotation = Quaternion.identity;
    [SerializeField]
    public WireRouteWinchData WinchData = null;
  }

  public class WireRouteData : ScriptableObject
  {
    [SerializeField]
    private List<WireRouteNodeData> m_data = new List<WireRouteNodeData>();

    public static string GetId( Wire wire, int counter )
    {
      return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name + "__" + wire.name + "__" + counter;
    }
    
    public bool Restore( WireRoute route )
    {
      foreach ( var data in m_data ) {
        var node = WireRouteNode.Create( data.NodeType, data.Parent, data.LocalPosition, data.LocalRotation );
        if ( data.NodeType == Wire.NodeType.WinchNode && data.WinchData != null ) {
          node.Winch.Speed           = data.WinchData.Speed;
          node.Winch.PulledInLength  = data.WinchData.PulledInLength;
          node.Winch.ForceRange      = data.WinchData.ForceRange;
          node.Winch.BrakeForceRange = data.WinchData.BrakeForceRange;
        }
        route.Add( node );
      }
      return true;
    }
  }
}
