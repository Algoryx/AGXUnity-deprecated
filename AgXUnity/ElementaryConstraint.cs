using System;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Base of controllers and object of ordinary elementary constraints.
  /// </summary>
  public class ElementaryConstraint : ScriptAsset
  {
    /// <summary>
    /// Create instance given temporary native elementary constraint.
    /// </summary>
    /// <param name="tmpEc">Temporary elementary constraint.</param>
    /// <returns>New instance, as similar as possible, to the given native elementary constraint.</returns>
    public static ElementaryConstraint Create( agx.ElementaryConstraint tmpEc )
    {
      if ( tmpEc == null )
        return null;

      ElementaryConstraint elementaryConstraint = null;

      // It's possible to know the type of controllers. We're basically not
      // interested in knowing the type of the ordinary ones.
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

      // This is a controller, instantiate the controller.
      if ( controllerType != null )
        elementaryConstraint = Create( Type.GetType( "AgXUnity." + controllerType.Name ) ) as ElementaryConstraint;
      // This is an ordinary elementary constraint.
      else
        elementaryConstraint = Create<ElementaryConstraint>();

      // Copies data from the native instance.
      elementaryConstraint.Construct( tmpEc );

      return elementaryConstraint;
    }

    /// <summary>
    /// Name of the native instance in the constraint. This is the
    /// link to our native instance as long as we have access to
    /// the native constraint.
    /// </summary>
    [SerializeField]
    private string m_nativeName = string.Empty;

    /// <summary>
    /// Name of the native instance in the constraint.
    /// </summary>
    [HideInInspector]
    public string NativeName { get { return m_nativeName; } }

    /// <summary>
    /// Enable flag. Paired with property Enable.
    /// </summary>
    [SerializeField]
    private bool m_enable = true;

    /// <summary>
    /// Enable flag.
    /// </summary>
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

    /// <summary>
    /// Number of rows in this elementary constraint.
    /// </summary>
    [HideInInspector]
    public int NumRows { get { return m_rowData.Length; } }

    /// <summary>
    /// Data (compliance, damping etc.) for each row in this elementary constraint.
    /// Paired with property RowData.
    /// </summary>
    [SerializeField]
    private ElementaryConstraintRowData[] m_rowData = null;

    /// <summary>
    /// Data (compliance, damping etc.) for each row in this elementary constraint.
    /// </summary>
    public ElementaryConstraintRowData[] RowData { get { return m_rowData; } }

    /// <summary>
    /// Native instance of this elementary constraint. Only set when the
    /// constraint is initialized and is simulating.
    /// </summary>
    public agx.ElementaryConstraint Native { get; private set; }

    /// <summary>
    /// Callback from Constraint when it's being initialized.
    /// </summary>
    /// <param name="constraint">Constraint object this elementary constraint is part of.</param>
    /// <returns>True if successful.</returns>
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

  /// <summary>
  /// Base class of controllers (such as motor, lock etc.).
  /// </summary>
  public class ElementaryConstraintController : ElementaryConstraint
  {
  }

  /// <summary>
  /// Range controller object, constraining the angle of the constraint to be
  /// within a given range.
  /// </summary>
  public class RangeController : ElementaryConstraintController
  {
    /// <summary>
    /// Valid range of the constraint angle. Paired with property Range.
    /// </summary>
    [SerializeField]
    private RangeReal m_range = new RangeReal();

    /// <summary>
    /// Valid range of the constraint angle.
    /// </summary>
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

  /// <summary>
  /// Target speed controller object, constraining the angle of the constraint to
  /// be driven at a given speed.
  /// </summary>
  public class TargetSpeedController : ElementaryConstraintController
  {
    /// <summary>
    /// Desired speed to drive the constraint angle. Paired with property Speed.
    /// </summary>
    [SerializeField]
    private float m_speed = 0f;

    /// <summary>
    /// Desired speed to drive the constraint angle.
    /// </summary>
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

    /// <summary>
    /// State to lock at the current angle when the speed is set to zero.
    /// Paired with property LockAtZeroSpeed.
    /// </summary>
    [SerializeField]
    private bool m_lockAtZeroSpeed = false;

    /// <summary>
    /// State to lock at the current angle when the speed is set to zero.
    /// </summary>
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

  /// <summary>
  /// Lock controller object, constraining the angle of the constraint to
  /// a given value.
  /// </summary>
  public class LockController : ElementaryConstraintController
  {
    /// <summary>
    /// Desired position/angle to lock the angle to. Paired with property Position.
    /// </summary>
    [SerializeField]
    private float m_position = 0f;

    /// <summary>
    /// Desired position/angle to lock the angle to.
    /// </summary>
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
