﻿using System;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Base of controllers and object of ordinary elementary constraints.
  /// </summary>
  [HideInInspector]
  [AddComponentMenu( "" )]
  public class ElementaryConstraint : ScriptComponent
  {
    /// <summary>
    /// Create instance given temporary native elementary constraint.
    /// </summary>
    /// <param name="tmpEc">Temporary elementary constraint.</param>
    /// <returns>New instance, as similar as possible, to the given native elementary constraint.</returns>
    public static ElementaryConstraint Create( GameObject gameObject, agx.ElementaryConstraint tmpEc )
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
        elementaryConstraint = gameObject.AddComponent( Type.GetType( "AgXUnity." + controllerType.Name ) ) as ElementaryConstraint;
      // This is an ordinary elementary constraint.
      else
        elementaryConstraint = gameObject.AddComponent<ElementaryConstraint>();

      // Copies data from the native instance.
      elementaryConstraint.Construct( tmpEc );

      return elementaryConstraint;
    }

    /// <summary>
    /// Takes this legacy elementary constraint, creates a new instance (added to gameObject) and
    /// copies all values/objects to the new instance.
    /// </summary>
    /// <param name="gameObject">Game object to add the new version of the elementary constraint to.</param>
    /// <returns>New added elementary constraint instance.</returns>
    public ElementaryConstraint FromLegacy( GameObject gameObject )
    {
      ElementaryConstraint target = gameObject.AddComponent( GetType() ) as ElementaryConstraint;
      target.Construct( this );

      return target;
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

    public void CopyFrom( ElementaryConstraint source )
    {
      Construct( source );
    }

    protected virtual void Construct( agx.ElementaryConstraint tmpEc )
    {
      m_nativeName = tmpEc.getName();
      m_enable     = tmpEc.getEnable();
      m_rowData    = new ElementaryConstraintRowData[ tmpEc.getNumRows() ];
      for ( uint i = 0; i < tmpEc.getNumRows(); ++i )
        m_rowData[ i ] = new ElementaryConstraintRowData( this, Convert.ToInt32( i ), tmpEc );
    }

    protected virtual void Construct( ElementaryConstraint source )
    {
      m_nativeName = source.m_nativeName;
      m_enable = source.m_enable;
      m_rowData = new ElementaryConstraintRowData[ source.NumRows ];
      for ( int i = 0; i < source.NumRows; ++i )
        m_rowData[ i ] = new ElementaryConstraintRowData( this, source.m_rowData[ i ] );
    }

    protected override bool Initialize()
    {
      if ( Native == null )
        return false;

      // Manually synchronizing data for native row coupling.
      foreach ( ElementaryConstraintRowData data in m_rowData )
        Utils.PropertySynchronizer.Synchronize( data );

      Utils.PropertySynchronizer.Synchronize( this );

      return true;
    }

    protected override void OnDestroy()
    {
      Native = null;

      base.OnDestroy();
    }
  }

  /// <summary>
  /// Base class of controllers (such as motor, lock etc.).
  /// </summary>
  [HideInInspector]
  public class ElementaryConstraintController : ElementaryConstraint
  {
    public T As<T>( Constraint.ControllerType controllerType ) where T : ElementaryConstraintController
    {
      bool typeMatch = GetType() == typeof( T );
      return typeMatch && IsControllerTypeMatch( controllerType ) ?
               this as T :
               null;
    }

    public Constraint.ControllerType GetControllerType()
    {
      return IsControllerTypeMatch( Constraint.ControllerType.Translational ) ?
               Constraint.ControllerType.Translational :
               Constraint.ControllerType.Rotational;
    }

    private bool IsControllerTypeMatch( Constraint.ControllerType controllerType )
    {
      return controllerType == Constraint.ControllerType.Primary ||
             ( controllerType == Constraint.ControllerType.Translational && NativeName.EndsWith( "T" ) ) ||
             ( controllerType == Constraint.ControllerType.Rotational && NativeName.EndsWith( "R" ) );
    }
  }

  /// <summary>
  /// Range controller object, constraining the angle of the constraint to be
  /// within a given range.
  /// </summary>
  [HideInInspector]
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

    protected override void Construct( agx.ElementaryConstraint tmpEc )
    {
      base.Construct( tmpEc );

      m_range = new RangeReal( agx.RangeController.safeCast( tmpEc ).getRange() );
    }

    protected override void Construct( ElementaryConstraint source )
    {
      base.Construct( source );

      m_range = new RangeReal( ( source as RangeController ).m_range );
    }
  }

  /// <summary>
  /// Target speed controller object, constraining the angle of the constraint to
  /// be driven at a given speed.
  /// </summary>
  [HideInInspector]
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

    protected override void Construct( agx.ElementaryConstraint tmpEc )
    {
      base.Construct( tmpEc );

      m_speed = Convert.ToSingle( agx.TargetSpeedController.safeCast( tmpEc ).getSpeed() );
      m_lockAtZeroSpeed = agx.TargetSpeedController.safeCast( tmpEc ).getLockedAtZeroSpeed();
    }

    protected override void Construct( ElementaryConstraint source )
    {
      base.Construct( source );

      m_speed           = ( source as TargetSpeedController ).m_speed;
      m_lockAtZeroSpeed = ( source as TargetSpeedController ).m_lockAtZeroSpeed;
    }
  }

  /// <summary>
  /// Lock controller object, constraining the angle of the constraint to
  /// a given value.
  /// </summary>
  [HideInInspector]
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

    protected override void Construct( agx.ElementaryConstraint tmpEc )
    {
      base.Construct( tmpEc );

      m_position = Convert.ToSingle( agx.LockController.safeCast( tmpEc ).getPosition() );
    }

    protected override void Construct( ElementaryConstraint source )
    {
      base.Construct( source );

      m_position = ( source as LockController ).m_position;
    }
  }

  [HideInInspector]
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

    protected override void Construct( agx.ElementaryConstraint tmpEc )
    {
      base.Construct( tmpEc );

      m_lead = Convert.ToSingle( agx.ScrewController.safeCast( tmpEc ).getLead() );
    }

    protected override void Construct( ElementaryConstraint source )
    {
      base.Construct( source );

      m_lead = ( source as ScrewController ).m_lead;
    }
  }

  [HideInInspector]
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

    protected override void Construct( ElementaryConstraint source )
    {
      base.Construct( source );

      m_voltage            = ( source as ElectricMotorController ).m_voltage;
      m_armatureResistance = ( source as ElectricMotorController ).m_armatureResistance;
      m_torqueConstant     = ( source as ElectricMotorController ).m_torqueConstant;
    }
  }
}
