using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgXUnity.Legacy
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

  public class WireRouteData : ScriptComponent
  {
    [SerializeField]
    private List<WireRouteNodeData> m_data = new List<WireRouteNodeData>();

    public bool Restore()
    {
      var route = GetComponent<WireRoute>();
      if ( route == null )
        return false;

      foreach ( var nodeData in m_data ) {
        var node = route.Add( nodeData.NodeType, nodeData.Parent, nodeData.LocalPosition, nodeData.LocalRotation );
        if ( node == null ) {
          Debug.LogWarning( "Unable to add node of type " + nodeData.NodeType + " to wire route.", route );
          continue;
        }

        if ( node.Type == Wire.NodeType.WinchNode && nodeData.WinchData != null ) {
          node.Winch.Speed           = nodeData.WinchData.Speed;
          node.Winch.PulledInLength  = nodeData.WinchData.PulledInLength;
          node.Winch.ForceRange      = new RangeReal( nodeData.WinchData.ForceRange );
          node.Winch.BrakeForceRange = new RangeReal( nodeData.WinchData.BrakeForceRange );
        }
      }

      return true;
    }
  }
}
