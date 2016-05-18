using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AgXUnity.Utils;
using UnityEngine;

namespace AgXUnity
{
  [AddComponentMenu( "" )]
  public class WireWinch : ScriptComponent
  {
    [HideInInspector]
    public agxWire.WireWinchController Native { get; private set; }

    [SerializeField]
    private float m_speed = 0.0f;
    public float Speed
    {
      get { return m_speed; }
      set
      {
        m_speed = value;
        if ( Native != null )
          Native.setSpeed( m_speed );
      }
    }

    [SerializeField]
    private float m_pulledInLength = 0.0f;
    [ClampAboveZeroInInspector( true )]
    public float PulledInLength
    {
      get { return m_pulledInLength; }
      set
      {
        m_pulledInLength = Mathf.Max( value, 0.0f );
        if ( Native != null )
          Native.setPulledInWireLength( m_pulledInLength );
      }
    }

    [SerializeField]
    private RangeReal m_forceRange = new RangeReal();
    public RangeReal ForceRange
    {
      get { return m_forceRange; }
      set
      {
        m_forceRange = value;
        if ( Native != null )
          Native.setForceRange( m_forceRange.Native );
      }
    }

    [SerializeField]
    private RangeReal m_brakeForceRange = new RangeReal() { Min = 0.0, Max = 0.0 };
    public RangeReal BrakeForceRange
    {
      get { return m_brakeForceRange; }
      set
      {
        m_brakeForceRange = value;
        if ( Native != null )
          Native.setBrakeForceRange( m_brakeForceRange.Native );
      }
    }

    public float CurrentForce
    {
      get
      {
        if ( Native != null )
          return Convert.ToSingle( Native.getCurrentForce() );
        return 0.0f;
      }
    }

    public Wire.RouteNode WinchNode
    {
      get
      {
        Wire wire = GetComponent<Wire>();
        if ( wire == null )
          return null;

        return wire.Route.Nodes.Find( node => node.Winch == this );
      }
    }

    protected override bool Initialize()
    {
      if ( WinchNode == null ) {
        Debug.LogWarning( "Unable to initialize winch - no winch node assigned.", this );
        return false;
      }

      RigidBody rb = WinchNode.Frame.Parent != null ? WinchNode.Frame.Parent.GetInitializedComponentInParent<RigidBody>() : null;
      if ( rb == null )
        Native = new agxWire.WireWinchController( null, WinchNode.Frame.Position.ToHandedVec3(), ( WinchNode.Frame.Rotation * Vector3.forward ).ToHandedVec3(), PulledInLength );
      else
        Native = new agxWire.WireWinchController( rb.Native, WinchNode.Frame.CalculateLocalPosition( rb.gameObject ).ToHandedVec3(), ( WinchNode.Frame.CalculateLocalRotation( rb.gameObject ) * Vector3.forward ).ToHandedVec3() );

      return base.Initialize();
    }

    protected void LateUpdate()
    {
      if ( Native != null )
        m_pulledInLength = Convert.ToSingle( Native.getPulledInWireLength() );
    }

    protected override void OnDestroy()
    {
      Native = null;

      base.OnDestroy();
    }
  }
}
