using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Data object holding compliance, damping and force range
  /// of a single constraint row.
  /// </summary>
  [AddComponentMenu( "" )]
  [System.Serializable]
  public class ConstraintRowData
  {
    public enum Def { Parameters = 0, U = 1 << 0, V = 1 << 1, N = 1 << 2, Linear = 1 << 3, Angular = 1 << 4 }

    [ClampAboveZeroInInspector( true )]
    public double Compliance = 1.0E-9;

    [ClampAboveZeroInInspector( true )]
    public double Damping = 2.5 / 50.0;

    public RangeReal ForceRange = new RangeReal();

    [HideInInspector]
    public Def Definition = Def.Parameters;

    [HideInInspector]
    public string DefinitionString
    {
      get
      {
        return ( Definition & ( Def.Linear | Def.Angular ) ).ToString() + " " + ( Definition & ( Def.U | Def.V | Def.N ) ).ToString();
      }
    }

    public void AssignTo( agx.ElementaryConstraint ec, int row )
    {
      ec.setCompliance( Compliance, row );
      ec.setDamping( Damping, row );
      ec.setForceRange( ForceRange.Native, (uint)row );
    }
  }

  /// <summary>
  /// Elementary constraint base class.
  /// </summary>
  [AddComponentMenu( "" )]
  public class ElementaryConstraint : ScriptComponent
  {
    #region Public Serialized Properties
    /// <summary>
    /// Compliance, damping and force range data for each row in
    /// this elementary constraint.
    /// </summary>
    public ConstraintRowData[] RowData = null;

    /// <summary>
    /// Called from inspector/custom editor when ConstraintRowData has updated values.
    /// </summary>
    public void OnRowDataChanged()
    {
      agx.ElementaryConstraint ec = FindNativeElementaryConstraint( NativeName );
      for ( int i = 0; ec != null && i < (int)ec.getNumRows(); ++i )
        RowData[ i ].AssignTo( ec, i );
    }

    /// <summary>
    /// Elementary constraint enable flag paired with property Enable.
    /// </summary>
    [SerializeField]
    private bool m_enable = true;

    /// <summary>
    /// Get or set enable.
    /// </summary>
    public bool Enable
    {
      get { return m_enable; }
      set
      {
        m_enable = value;
        agx.ElementaryConstraint ec = FindNativeElementaryConstraint( m_nativeName );
        if ( ec != null )
          ec.setEnable( m_enable );
      }
    }

    /// <summary>
    /// Name of this elementary constraint paired with property NativeName.
    /// </summary>
    [SerializeField]
    private string m_nativeName = "";

    /// <summary>
    /// Get or set the name of this elementary constraint.
    /// </summary>
    public string NativeName
    {
      get { return m_nativeName; }
      set
      {
        if ( m_nativeName == value )
          return;

        // Synchronize name with the native object if it exist.
        agx.ElementaryConstraint ec = FindNativeElementaryConstraint( m_nativeName );
        m_nativeName = value;
        if ( ec != null )
          ec.setName( m_nativeName );
      }
    }
    #endregion

    /// <summary>
    /// Number of rows assigned during construction.
    /// </summary>
    [SerializeField]
    private int m_numRows = 0;

    /// <summary>
    /// Get the number of rows in this elementary constraint.
    /// </summary>
    public int NumRows
    {
      get { return m_numRows; }
    }

    /// <summary>
    /// Construct given number of rows and enable flag. Secondary
    /// elementary constraints are in general disabled during
    /// construct.
    /// </summary>
    /// <param name="numRows">Number of rows in this elementary constraint.</param>
    /// <param name="enable">True if enabled.</param>
    protected ElementaryConstraint( int numRows, bool enable = true )
    {
      m_numRows = numRows;
      m_enable = enable;
    }

    /// <summary>
    /// Finds the constraint object that this elementary
    /// constraint is part of.
    /// </summary>
    protected AgXUnity.Constraint Constraint
    {
      get
      {
        ScriptComponent[] components = GetComponents<ScriptComponent>();
        foreach ( ScriptComponent component in components ) {
          Constraint constraint = component as Constraint;
          if ( constraint != null )
            return constraint;
        }

        return null;
      }
    }

    /// <summary>
    /// Finds the native constraint instance that this elementary
    /// constraint is part of - if created.
    /// </summary>
    protected agx.Constraint NativeConstraint
    {
      get
      {
        Constraint constraint = Constraint;
        if ( constraint != null )
          return constraint.Native;
        return null;
      }
    }

    /// <summary>
    /// Finds native instance of a controller given name.
    /// </summary>
    /// <typeparam name="T">Controller type.</typeparam>
    /// <param name="name">Name of the controller.</param>
    /// <returns>Controller of type name T.</returns>
    protected T FindNativeSecondaryConstraint<T>( string name ) where T : agx.ElementaryConstraint
    {
      agx.Constraint native = NativeConstraint;
      if ( native == null )
        return null;

      // Not possible to cast from agx.ElementaryConstraint. We have
      // to search given name and do explicit tests.
      agx.ElementaryConstraint sc = native.getSecondaryConstraintGivenName( name );

      if ( sc == null )
        return null;

      Type type = typeof( T );
      if ( type == typeof( agx.RangeController ) )
        return agx.RangeController.safeCast( sc ) as T;
      else if ( type == typeof( agx.TargetSpeedController ) )
        return agx.TargetSpeedController.safeCast( sc ) as T;
      else if ( type == typeof( agx.LockController ) )
        return agx.LockController.safeCast( sc ) as T;
      else if ( type == typeof( agx.ScrewController ) )
        return agx.ScrewController.safeCast( sc ) as T;
      return null;
    }

    /// <summary>
    /// Searches for native elementary- OR secondary constraint with the given name.
    /// </summary>
    /// <param name="name">Name of the elementary- or secondary constraint.</param>
    /// <returns>Elementary- or secondary constraint if found - otherwise null.</returns>
    protected agx.ElementaryConstraint FindNativeElementaryConstraint( string name )
    {
      agx.Constraint native = NativeConstraint;
      if ( native == null )
        return null;
      return native.getElementaryConstraintGivenName( name ) ?? native.getSecondaryConstraintGivenName( name );
    }

    /// <summary>
    /// Moves data from RowData to the native instance.
    /// </summary>
    protected bool InitializeData()
    {
      agx.ElementaryConstraint ec = FindNativeElementaryConstraint( NativeName );
      for ( int i = 0; ec != null && i < NumRows; ++i )
        RowData[ i ].AssignTo( ec, i );
      return base.Initialize();
    }

    protected override bool Initialize()
    {
      // It's up to the constraint to initialize its elementary constraints.
      // We are a component, so if Unity tries to initialize us before the
      // constraint, wait for the constraint (or never initialize because
      // it's undefined).
      Constraint constraint = Constraint;
      return constraint != null && constraint.GetInitialized<Constraint>() != null && InitializeData();
    }
  }

  /// <summary>
  /// Quat lock elementary constraint - three rows.
  /// </summary>
  [AddComponentMenu( "" )]
  public class ElementaryQuatLock : ElementaryConstraint
  {
    public ElementaryQuatLock()
      : base( 3 )
    {
    }
  }

  /// <summary>
  /// Elementary spherical constraint - three rows.
  /// </summary>
  [AddComponentMenu( "" )]
  public class ElementarySphericalRel : ElementaryConstraint
  {
    public ElementarySphericalRel()
      : base( 3 )
    {
    }
  }

  /// <summary>
  /// Elementary dot 2 constraint - one row.
  /// </summary>
  [AddComponentMenu( "" )]
  public class ElementaryDot2 : ElementaryConstraint
  {
    public ElementaryDot2()
      : base( 1 )
    {
    }
  }

  /// <summary>
  /// Elementary dot 1 constraint - one row.
  /// </summary>
  [AddComponentMenu( "" )]
  public class ElementaryDot1 : ElementaryConstraint
  {
    public ElementaryDot1()
      : base( 1 )
    {
    }
  }

  /// <summary>
  /// Elementary contact normal constraint - one row.
  /// </summary>
  [AddComponentMenu( "" )]
  public class ElementaryContactNormal : ElementaryConstraint
  {
    public ElementaryContactNormal()
      : base( 1 )
    {
    }
  }

  /// <summary>
  /// Range controller - one row, disabled by default.
  /// </summary>
  [AddComponentMenu( "" )]
  public class RangeController : ElementaryConstraint
  {
    /// <summary>
    /// Working range, paired with property Range.
    /// </summary>
    [SerializeField]
    private RangeReal m_range = new RangeReal();

    /// <summary>
    /// Get or set working range of this range controller.
    /// </summary>
    public RangeReal Range
    {
      get { return m_range; }
      set
      {
        m_range = value;

        // Synchronize with native instance.
        agx.RangeController native = FindNativeSecondaryConstraint<agx.RangeController>( NativeName );
        if ( native != null ) {
          native.setEnable( Enable );
          native.setRange( m_range.Native );
        }
      }
    }

    public RangeController()
      : base( 1, false ) { }
  }

  /// <summary>
  /// Target speed controller - one row, disabled by default.
  /// </summary>
  [AddComponentMenu( "" )]
  public class TargetSpeedMotor : ElementaryConstraint
  {
    /// <summary>
    /// Speed of the controller, paired with property Speed.
    /// </summary>
    [SerializeField]
    private float m_speed = 0.0f;

    /// <summary>
    /// Get or set speed of this controller.
    /// </summary>
    public float Speed
    {
      get { return m_speed; }
      set
      {
        m_speed = value;

        // Synchronize with native instance.
        agx.TargetSpeedController native = FindNativeSecondaryConstraint<agx.TargetSpeedController>( NativeName );
        if ( native != null ) {
          native.setEnable( Enable );
          native.setSpeed( m_speed );
        }
      }
    }

    public TargetSpeedMotor()
      : base( 1, false ) { }
  }

  /// <summary>
  /// Lock controller - one row, disabled by default.
  /// </summary>
  [AddComponentMenu( "" )]
  public class LockController : ElementaryConstraint
  {
    /// <summary>
    /// Position of the lock, paired with property Position.
    /// </summary>
    [SerializeField]
    private float m_position = 0.0f;

    /// <summary>
    /// Get or set position of the lock.
    /// </summary>
    public float Position
    {
      get { return m_position; }
      set
      {
        m_position = value;

        // Synchronize with native instance.
        agx.LockController native = FindNativeSecondaryConstraint<agx.LockController>( NativeName );
        if ( native != null ) {
          native.setEnable( Enable );
          native.setPosition( m_position );
        }
      }
    }

    public LockController()
      : this( false ) { }

    /// <summary>
    /// Constructor used by the distance joint which has
    /// this controller enabled by default.
    /// </summary>
    public LockController( bool enable )
      : base( 1, enable ) { }
  }

  /// <summary>
  /// Elementary constraint helper factory to build the different constraints.
  /// This object holds elementary constraint type, name and row definition
  /// (direction, rotational|translational).
  /// </summary>
  public struct ElementaryDef
  {
    public Type Type;
    public string Name;
    public List<ConstraintRowData.Def> Variables;

    private static ElementaryDef Create<T>( string name, List<ConstraintRowData.Def> vars ) where T : ElementaryConstraint
    {
      return new ElementaryDef()
      {
        Type = typeof( T ),
        Name = name,
        Variables = vars
      };
    }

    private static ElementaryDef Create<T>( string name, ConstraintRowData.Def variable ) where T : ElementaryConstraint
    {
      return Create<T>( name, new List<ConstraintRowData.Def>()
                              {
                                variable | ConstraintRowData.Def.U,
                                variable | ConstraintRowData.Def.V,
                                variable | ConstraintRowData.Def.N,
                              } );
    }

    public static ElementaryDef CreateLinear<T>( string name ) where T : ElementaryConstraint
    {
      return Create<T>( name, ConstraintRowData.Def.Linear );
    }

    public static ElementaryDef CreateAngular<T>( string name ) where T : ElementaryConstraint
    {
      return Create<T>( name, ConstraintRowData.Def.Angular );
    }

    public static ElementaryDef CreateLinear<T>( string name, ConstraintRowData.Def axis ) where T : ElementaryConstraint
    {
      return Create<T>( name, new List<ConstraintRowData.Def>() { ConstraintRowData.Def.Linear | axis } );
    }

    public static ElementaryDef CreateAngular<T>( string name, ConstraintRowData.Def axis ) where T : ElementaryConstraint
    {
      return Create<T>( name, new List<ConstraintRowData.Def>() { ConstraintRowData.Def.Angular | axis } );
    }

    public static ElementaryDef CreateLinear<T>( string name, ConstraintRowData.Def axis1, ConstraintRowData.Def axis2 ) where T : ElementaryConstraint
    {
      return Create<T>( name, new List<ConstraintRowData.Def>() { ConstraintRowData.Def.Linear | axis1, ConstraintRowData.Def.Linear | axis2 } );
    }

    public static ElementaryDef CreateAngular<T>( string name, ConstraintRowData.Def axis1, ConstraintRowData.Def axis2 ) where T : ElementaryConstraint
    {
      return Create<T>( name, new List<ConstraintRowData.Def>() { ConstraintRowData.Def.Angular | axis1, ConstraintRowData.Def.Angular | axis2 } );
    }
  }
}
