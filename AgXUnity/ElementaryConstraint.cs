using System;
using UnityEngine;

namespace AgXUnity
{
  [AddComponentMenu( "" )]
  [Serializable]
  public class ElementaryConstraintRowData
  {
    [SerializeField]
    private int m_row = -1;
    [HideInInspector]
    public int Row { get { return m_row; } }
    [HideInInspector]
    public ulong RowUInt64 { get { return Convert.ToUInt64( Row ); } }

    [SerializeField]
    private ElementaryConstraint m_elementaryConstraint = null;
    [HideInInspector]
    public ElementaryConstraint ElementaryConstraint { get { return m_elementaryConstraint; } }

    [SerializeField]
    private float m_compliance = 1.0E-10f;
    [ClampAboveZeroInInspector( true )]
    public float Compliance
    {
      get { return m_compliance; }
      set
      {
        m_compliance = value;
        if ( ElementaryConstraint.Native != null )
          ElementaryConstraint.Native.setCompliance( m_compliance, Row );
      }
    }

    [SerializeField]
    private float m_damping = 2.0f / 60.0f;
    [ClampAboveZeroInInspector( true )]
    public float Damping
    {
      get { return m_damping; }
      set
      {
        m_damping = value;
        if ( ElementaryConstraint.Native != null )
          ElementaryConstraint.Native.setDamping( m_damping, Row );
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
        if ( ElementaryConstraint.Native != null )
          ElementaryConstraint.Native.setForceRange( m_forceRange.Native, RowUInt64 );
      }
    }

    public ElementaryConstraintRowData( ElementaryConstraint elementaryConstraint, int row, agx.ElementaryConstraint tmpEc = null )
    {
      m_elementaryConstraint = elementaryConstraint;
      m_row = row;
      if ( tmpEc != null ) {
        m_compliance = Convert.ToSingle( tmpEc.getCompliance( RowUInt64 ) );
        m_damping = Convert.ToSingle( tmpEc.getDamping( RowUInt64 ) );
        m_forceRange = new RangeReal( tmpEc.getForceRange( RowUInt64 ) );
      }
    }
  }

  public class ElementaryConstraint : ScriptAsset
  {
    public static ElementaryConstraint Create( agx.ElementaryConstraint tmpEc )
    {
      ElementaryConstraint elementaryConstraint = null;
      Type controllerType = null;
      if ( agx.RangeController.safeCast( tmpEc ) != null )
        controllerType = agx.RangeController.safeCast( tmpEc ).GetType();
      else if ( agx.TargetSpeedController.safeCast( tmpEc ) != null )
        controllerType = agx.TargetSpeedController.safeCast( tmpEc ).GetType();
      else if ( agx.LockController.safeCast( tmpEc ) != null )
        controllerType = agx.LockController.safeCast( tmpEc ).GetType();
      else if ( agx.ScrewController.safeCast( tmpEc ) != null )
        controllerType = agx.ScrewController.safeCast( tmpEc ).GetType();
      else if ( agx.ElectricMotorController.safeCast( tmpEc ) != null )
        controllerType = agx.ElectricMotorController.safeCast( tmpEc ).GetType();

      if ( controllerType != null )
        elementaryConstraint = Create( Type.GetType( "AgXUnity." + controllerType.Name ) ) as ElementaryConstraint;
      else
        elementaryConstraint = Create<ElementaryConstraint>();

      elementaryConstraint.Construct( tmpEc );

      return elementaryConstraint;
    }

    [SerializeField]
    private string m_nativeName = string.Empty;

    [HideInInspector]
    public string NativeName { get { return m_nativeName; } }

    [SerializeField]
    private bool m_enable = true;
    [HideInInspector]
    public bool Enable
    {
      get { return m_enable; }
      set
      {
        m_enable = value;
        if ( Native != null )
          Native.setEnable( m_enable );
      }
    }

    [HideInInspector]
    public int NumRows { get { return m_rowData.Length; } }

    [SerializeField]
    private ElementaryConstraintRowData[] m_rowData = null;
    public ElementaryConstraintRowData[] RowData { get { return m_rowData; } }

    public agx.ElementaryConstraint Native { get; private set; }

    public virtual bool OnConstraintInitialize( Constraint constraint )
    {
      Native = constraint.Native.getElementaryConstraintGivenName( NativeName ) ??
               constraint.Native.getSecondaryConstraintGivenName( NativeName );

      return GetInitialized<ElementaryConstraint>() != null;
    }

    protected virtual void Construct( agx.ElementaryConstraint tmpEc )
    {
      m_nativeName = tmpEc.getName();
      m_enable     = tmpEc.getEnable();
      m_rowData    = new ElementaryConstraintRowData[ tmpEc.getNumRows() ];
      for ( ulong i = 0; i < tmpEc.getNumRows(); ++i )
        m_rowData[ i ] = new ElementaryConstraintRowData( this, Convert.ToInt32( i ), tmpEc );
    }

    protected override void Construct()
    {
    }

    protected override bool Initialize()
    {
      if ( Native == null )
        return false;

      // Manually synchronizing data for native row coupling.
      foreach ( ElementaryConstraintRowData data in m_rowData )
        Utils.PropertySynchronizer.Synchronize( data );

      return true;
    }

    public override void Destroy()
    {
      Native = null;
    }
  }

  public class ElementaryConstraintController : ElementaryConstraint
  {
  }

  public class RangeController : ElementaryConstraintController
  {
    [SerializeField]
    private RangeReal m_range = new RangeReal();
    public RangeReal Range
    {
      get { return m_range; }
      set
      {
        m_range = value;
        if ( Native != null )
          agx.RangeController.safeCast( Native ).setRange( m_range.Native );
      }
    }
  }

  public class TargetSpeedController : ElementaryConstraintController
  {
    [SerializeField]
    private float m_speed = 0f;
    public float Speed
    {
      get { return m_speed; }
      set
      {
        m_speed = value;
        if ( Native != null )
          agx.TargetSpeedController.safeCast( Native ).setSpeed( m_speed );
      }
    }

    [SerializeField]
    private bool m_lockAtZeroSpeed = false;
    public bool LockAtZeroSpeed
    {
      get { return m_lockAtZeroSpeed; }
      set
      {
        m_lockAtZeroSpeed = value;
        if ( Native != null )
          agx.TargetSpeedController.safeCast( Native ).setLockedAtZeroSpeed( m_lockAtZeroSpeed );
      }
    }
  }

  public class LockController : ElementaryConstraintController
  {
    [SerializeField]
    private float m_position = 0f;
    public float Position
    {
      get { return m_position; }
      set
      {
        m_position = value;
        if ( Native != null )
          agx.LockController.safeCast( Native ).setPosition( m_position );
      }
    }
  }

  public class ScrewController : ElementaryConstraintController
  {
    [SerializeField]
    private float m_lead = 0f;
    public float Lead
    {
      get { return m_lead; }
      set
      {
        m_lead = value;
        if ( Native != null )
          agx.ScrewController.safeCast( Native ).setLead( m_lead );
      }
    }
  }

  public class ElectricMotorController : ElementaryConstraintController
  {
    [SerializeField]
    private float m_voltage = 0f;
    public float Voltage
    {
      get { return m_voltage; }
      set
      {
        m_voltage = value;
        if ( Native != null )
          agx.ElectricMotorController.safeCast( Native ).setVoltage( m_voltage );
      }
    }

    [SerializeField]
    private float m_armatureResistance = 0f;
    public float ArmatureResistance
    {
      get { return m_armatureResistance; }
      set
      {
        m_armatureResistance = value;
        if ( Native != null )
          agx.ElectricMotorController.safeCast( Native ).setArmatureResistance( m_armatureResistance );
      }
    }

    [SerializeField]
    private float m_torqueConstant = 0f;
    public float TorqueConstant
    {
      get { return m_torqueConstant; }
      set
      {
        m_torqueConstant = value;
        if ( Native != null )
          agx.ElectricMotorController.safeCast( Native ).setTorqueConstant( m_torqueConstant );
      }
    }

    protected override void Construct( agx.ElementaryConstraint tmpEc )
    {
      base.Construct( tmpEc );

      m_voltage            = Convert.ToSingle( agx.ElectricMotorController.safeCast( tmpEc ).getVoltage() );
      m_armatureResistance = Convert.ToSingle( agx.ElectricMotorController.safeCast( tmpEc ).getArmatureResistance() );
      m_torqueConstant     = Convert.ToSingle( agx.ElectricMotorController.safeCast( tmpEc ).getTorqueConstant() );
    }
  }
}
