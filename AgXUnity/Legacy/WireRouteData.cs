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

    public void Save()
    {
      hideFlags = HideFlags.HideInInspector;

      m_data.Clear();

      var wire = GetComponent<Wire>();
      if ( wire == null )
        return;

      foreach ( var node in wire.Route ) {
        m_data.Add( new WireRouteNodeData()
        {
          NodeType      = node.Type,
          Parent        = node.Frame.Parent,
          LocalPosition = node.Frame.LocalPosition,
          LocalRotation = node.Frame.LocalRotation,
          WinchData     = node.Type == Wire.NodeType.WinchNode ?
                          new WireRouteWinchData()
                          {
                            Speed = node.Winch.Speed,
                            PulledInLength = node.Winch.PulledInLength,
                            ForceRange = node.Winch.ForceRange,
                            BrakeForceRange = node.Winch.BrakeForceRange
                          } :
                          null
        } );
      }
    }
  }
}
