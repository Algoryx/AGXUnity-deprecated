using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AgXUnity.Utils;
using UnityEngine;

namespace AgXUnity
{
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
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

    [SerializeField]
    private Vector3 m_localPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 LocalPosition
    {
      get { return m_localPosition; }
      set { m_localPosition = value; }
    }

    [SerializeField]
    private Vector3 m_localDirection = Vector3.forward;
    [HideInInspector]
    public Vector3 LocalDirection
    {
      get { return m_localDirection; }
      set { m_localDirection = value; }
    }

    [SerializeField]
    private GameObject m_parent = null;
    [HideInInspector]
    public GameObject Parent
    {
      get { return m_parent; }
      set { m_parent = value; }
    }

    protected override bool Initialize()
    {
      RigidBody rb = Parent.GetInitializedComponentInParent<RigidBody>();
      Native = new agxWire.WireWinchController( rb != null ? rb.Native : null, LocalPosition.AsVec3(), LocalDirection.AsVec3() );

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
