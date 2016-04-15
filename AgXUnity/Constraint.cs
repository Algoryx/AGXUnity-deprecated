using System;
using System.Collections.Generic;
using AgXUnity.Utils;
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
  [GenerateCustomEditor]
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
  [GenerateCustomEditor]
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
  [GenerateCustomEditor]
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
  [GenerateCustomEditor]
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
  [GenerateCustomEditor]
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
  [GenerateCustomEditor]
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
  [GenerateCustomEditor]
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
  [GenerateCustomEditor]
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
    public Type type;
    public string name;
    public List<ConstraintRowData.Def> variables;

    private static ElementaryDef Create<T>( string name, List<ConstraintRowData.Def> vars ) where T : ElementaryConstraint
    {
      return new ElementaryDef()
      {
        type = typeof( T ),
        name = name,
        variables = vars
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

  /// <summary>
  /// Constraint base class.
  /// </summary>
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
  public abstract class Constraint : ScriptComponent
  {
    /// <summary>
    /// Native instance.
    /// </summary>
    protected agx.Constraint m_constraint = null;

    /// <summary>
    /// Constraint frames are game objects. It's not
    /// possible to change these after instantiation.
    /// </summary>
    [SerializeField]
    protected GameObject[] m_frames = null;

    /// <summary>
    /// Type of the native constraint.
    /// </summary>
    [SerializeField]
    protected Type m_nativeType = null;

    /// <summary>
    /// Access to native constraint.
    /// </summary>
    /// <remarks>
    /// This object is created after the initialize call.
    /// </remarks>
    public agx.Constraint Native { get { return m_constraint; } }

    /// <summary>
    /// First constraint frame.
    /// </summary>
    public GameObject Frame1 { get { return m_frames[ 0 ]; } }

    /// <summary>
    /// Second constraint frame.
    /// </summary>
    public GameObject Frame2 { get { return m_frames[ 1 ]; } }

    /// <summary>
    /// First rigid body.
    /// </summary>
    public RigidBody Body1 { get { return Find.FirstParentWithComponent<RigidBody>( Frame1 ); } }

    /// <summary>
    /// Second rigid body.
    /// </summary>
    public RigidBody Body2 { get { return Find.FirstParentWithComponent<RigidBody>( Frame2 ); } }

    /// <summary>
    /// True if valid setup given current game objects.
    /// </summary>
    public bool Valid
    {
      get
      {
        return m_frames != null && Body1 != null && Frame2 != null;
      }
    }

    /// <summary>
    /// Assign compliance to all elementary constraints (including controllers/secondary constraints).
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public double Compliance
    {
      get
      {
        ElementaryConstraint[] ecs = GetComponents<ElementaryConstraint>();
        if ( ecs == null || ecs.Length == 0 )
          return -1.0;
        double refVal = ecs[ 0 ].RowData[ 0 ].Compliance;
        foreach ( ElementaryConstraint ec in ecs ) {
          foreach ( ConstraintRowData rowData in ec.RowData )
            if ( rowData.Compliance != refVal )
              return -1.0;
        }

        return refVal;
      }
      set
      {
        ElementaryConstraint[] ecs = GetComponents<ElementaryConstraint>();
        if ( ecs == null || ecs.Length == 0 )
          return;
        foreach ( ElementaryConstraint ec in ecs )
          foreach ( ConstraintRowData rowData in ec.RowData )
            rowData.Compliance = value;
      }
    }

    /// <summary>
    /// Assign damping to all elementary constraints (including controllers/secondary constraints).
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public double Damping
    {
      get
      {
        ElementaryConstraint[] ecs = GetComponents<ElementaryConstraint>();
        if ( ecs == null || ecs.Length == 0 )
          return -1.0;
        double refVal = ecs[ 0 ].RowData[ 0 ].Damping;
        foreach ( ElementaryConstraint ec in ecs ) {
          foreach ( ConstraintRowData rowData in ec.RowData )
            if ( rowData.Damping != refVal )
              return -1.0;
        }

        return refVal;
      }
      set
      {
        ElementaryConstraint[] ecs = GetComponents<ElementaryConstraint>();
        if ( ecs == null || ecs.Length == 0 )
          return;
        foreach ( ElementaryConstraint ec in ecs )
          foreach ( ConstraintRowData rowData in ec.RowData )
            rowData.Damping = value;
      }
    }

    /// <summary>
    /// Create constraint given two frames (game objects). The first frame has to be child
    /// to another game object with a rigid body component. If the second frame isn't child
    /// to another game object with a rigid body component, the constraint will be attached
    /// in world.
    /// </summary>
    /// <typeparam name="T">Type of constraint, e.g., Hinge, Prismatic, BallJoint etc..</typeparam>
    /// <param name="frame1">First frame, child to another game object with a rigid body component.</param>
    /// <param name="frame2">Second frame.</param>
    /// <returns>Configured constraint, given <paramref name="frame1"/> and <paramref name="frame2"/>.</returns>
    /// <example>
    /// GameObject rb1 = ( new GameObject( "rb1" ) ).AddComponent{RigidBody}().gameObject;
    /// GameObject frame1 = new GameObject( "frame1" );
    /// rb1.AddChild( frame1 );
    /// 
    /// GameObject frame2 = new GameObject( "frame2" );
    /// 
    /// Constraint constriant = Constraint.Create{Hinge}( frame1, fram2 );
    /// </example>
    public static T Create<T>( GameObject frame1, GameObject frame2 ) where T : Constraint
    {
      if ( frame1 == null || frame2 == null )
        return null;

      RigidBody rb1 = frame1.GetComponentInParent<RigidBody>();
      if ( rb1 == null ) {
        Debug.LogWarning( "First constraint object must contain RigidBody component. Select the objects in a different order." );
        return null;
      }

      RigidBody rb2 = frame2.GetComponentInParent<RigidBody>();
      GameObject constraintGameObject = new GameObject( CreateName<T>( rb1, rb2 ) );
      constraintGameObject.isStatic = true;
      T constraint = constraintGameObject.AddComponent<T>();

      var elements = constraint.GetElements();
      foreach ( var element in elements ) {
        ElementaryConstraint ec = (ElementaryConstraint)constraintGameObject.AddComponent( element.type );
        ec.NativeName = element.name;
        ec.RowData = new ConstraintRowData[ ec.NumRows ];
        for ( int i = 0; i < ec.RowData.Length; ++i )
          ec.RowData[ i ] = new ConstraintRowData();

        // Check if the element supports enhanced editor description.
        for ( int i = 0; ec.NumRows == element.variables.Count && i < ec.NumRows; ++i )
          ec.RowData[ i ].Definition = element.variables[ i ];
      }

      constraint.m_frames = new[] { frame1, frame2 };
      constraint.PostCreate();

      return constraint;
    }

    /// <summary>
    /// Creates unique name "rb1NameRb2NameConstraintType", e.g., rb1Rb2Hinge.
    /// </summary>
    /// <typeparam name="T">Constraint type.</typeparam>
    /// <param name="rb1">First rigid body.</param>
    /// <param name="rb2">Second rigid body.</param>
    /// <returns>Unique constraint name.</returns>
    public static string CreateName<T>( RigidBody rb1, RigidBody rb2 ) where T : Constraint
    {
      string cName = ( rb1 != null ? rb1.name : "world" ) + ( rb2 != null ? rb2.name.FirstCharToUpperCase() : "World" );
      return Factory.CreateName( cName + typeof( T ).Name );
    }

    /// <summary>
    /// Destroys frames and the game object with this constraint component.
    /// </summary>
    public void Destroy( bool immediate )
    {
      if ( immediate ) {
        GameObject.DestroyImmediate( Frame1 );
        GameObject.DestroyImmediate( Frame2 );
        GameObject.DestroyImmediate( gameObject );
      }
      else {
        GameObject.Destroy( Frame1 );
        GameObject.Destroy( Frame2 );
        GameObject.Destroy( gameObject );
      }
    }

    /// <summary>
    /// Creates native instance and adds it to the simulation.
    /// </summary>
    protected override bool Initialize()
    {
      if ( m_frames == null || m_frames[ 0 ] == null || m_frames[ 1 ] == null ) {
        Debug.LogWarning( "Constraint '" + name + "': Never constructed, no reference frames assigned." );
        return false;
      }

      RigidBody rb1 = m_frames[ 0 ].GetInitializedComponentInParent<RigidBody>();
      RigidBody rb2 = m_frames[ 1 ].GetInitializedComponentInParent<RigidBody>();

      if ( rb1.Native == null ) {
        Debug.LogWarning( "Constraint '" + name + "': First constraint object must contain RigidBody component." );
        return false;
      }

      agx.Frame f1 = new agx.Frame();
      agx.Frame f2 = new agx.Frame();

      if ( m_frames[ 0 ].transform != rb1.transform ) {
        f1.setLocalTranslate( ( rb1.transform.InverseTransformPoint( m_frames[ 0 ].transform.position ) ).AsVec3() );
        f1.setLocalRotate( ( Quaternion.Inverse( rb1.transform.rotation ) * m_frames[ 0 ].transform.rotation ).AsQuat() );
      }

      if ( rb2 != null && m_frames[ 1 ].transform != rb2.transform ) {
        f2.setLocalTranslate( ( rb2.transform.InverseTransformPoint( m_frames[ 1 ].transform.position ).AsVec3() ) );
        f2.setLocalRotate( ( Quaternion.Inverse( rb2.transform.rotation ) * m_frames[ 1 ].transform.rotation ).AsQuat() );
      }
      else {
        f2.setLocalTranslate( m_frames[ 1 ].transform.position.AsVec3() );
        f2.setLocalRotate( m_frames[ 1 ].transform.rotation.AsQuat() );
      }

      m_constraint = Instantiate( rb1.Native, f1, rb2 ? rb2.Native : null, f2 );
      if ( m_constraint == null )
        return false;

      GetSimulation().add( m_constraint );

      if ( !m_constraint.getValid() )
        Debug.LogWarning( "Constraint '" + name + "': Not fully initialized." );

      return m_constraint.getValid() && base.Initialize();
    }

    /// <summary>
    /// Unity fixed update callback.
    /// </summary>
    protected void FixedUpdate()
    {
      if ( m_constraint == null )
        return;

      // Update native constraint frames given current "game object frame" transform.
      agx.Frame f1 = m_constraint.getAttachment( m_constraint.getBodyAt( 0 ) ).getFrame();
      agx.Frame f2 = m_constraint.getAttachment( m_constraint.getBodyAt( 1 ) ).getFrame();

      f1.setLocalTranslate( m_frames[ 0 ].transform.localPosition.AsVec3() );
      f1.setLocalRotate( m_frames[ 0 ].transform.localRotation.AsQuat() );

      f2.setLocalTranslate( m_frames[ 1 ].transform.localPosition.AsVec3() );
      f2.setLocalRotate( m_frames[ 1 ].transform.localRotation.AsQuat() );

      // If we've an animator object attached to this constraint,
      // synchronize properties.
      UnityEngine.Animator animator = GetComponent<UnityEngine.Animator>();
      if ( animator != null ) {
        ScriptComponent[] components = GetComponents<ScriptComponent>();
        foreach ( ScriptComponent component in components )
          Utils.PropertySynchronizer.Synchronize( component );
      }
    }

    /// <summary>
    /// Unity destroy callback.
    /// </summary>
    protected override void OnDestroy()
    {
      if ( GetSimulation() != null )
        GetSimulation().remove( m_constraint );

      m_constraint = null;

      GameObject.Destroy( m_frames[ 0 ] );
      GameObject.Destroy( m_frames[ 1 ] );

      base.OnDestroy();
    }

    /// <summary>
    /// Instantiates native object given native type (assigned in each constraint type implementation below).
    /// </summary>
    /// <param name="rb1">First rigid body.</param>
    /// <param name="f1">First constraint frame.</param>
    /// <param name="rb2">Second rigid body.</param>
    /// <param name="f2">Second constraint frame.</param>
    /// <returns>An instance of the native constraint if the configuration is valid.</returns>
    protected virtual agx.Constraint Instantiate( agx.RigidBody rb1, agx.Frame f1, agx.RigidBody rb2, agx.Frame f2 )
    {
      try {
        return (agx.Constraint)Activator.CreateInstance( m_nativeType, new object[] { rb1, f1, rb2, f2 } );
      }
      catch ( System.Exception e ) {
        Debug.LogWarning( "Unable to instantiate constraint of type: " + m_nativeType + " with general constructor (rb1, frame1, rb2, frame2)" );
        Debug.LogException( e );
      }
      return null;
    }

    /// <summary>
    /// Abstract method where each implementation must provide a list
    /// of elements (elementary constraint types etc.).
    /// </summary>
    /// <returns>List of elements.</returns>
    protected abstract List<ElementaryDef> GetElements();

    /// <summary>
    /// Post create method, called when the constraint has a native instance.
    /// </summary>
    protected virtual void PostCreate() {}
  }

  /// <summary>
  /// Hinge constraint with range, motor and lock.
  /// </summary>
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
  public class Hinge : Constraint
  {
    protected Hinge() { m_nativeType = typeof( agx.Hinge ); }

    protected override List<ElementaryDef> GetElements()
    {
      return new List<ElementaryDef>
        {
          ElementaryDef.CreateLinear<ElementarySphericalRel>( "SR" ),
          ElementaryDef.CreateAngular<ElementaryDot1>( "D1_VN", ConstraintRowData.Def.V ),
          ElementaryDef.CreateAngular<ElementaryDot1>( "D1_UN", ConstraintRowData.Def.U ),
          ElementaryDef.CreateAngular<RangeController>( "RR", ConstraintRowData.Def.N ),
          ElementaryDef.CreateAngular<TargetSpeedMotor>( "MR", ConstraintRowData.Def.N ),
          ElementaryDef.CreateAngular<LockController>( "LR", ConstraintRowData.Def.N )
        };
    }
  }

  /// <summary>
  /// Prismatic constraint with range, motor and lock.
  /// </summary>
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
  public class Prismatic : Constraint
  {
    protected Prismatic() { m_nativeType = typeof( agx.Prismatic ); }

    protected override List<ElementaryDef> GetElements()
    {
      return new List<ElementaryDef>
        {
          ElementaryDef.CreateAngular<ElementaryQuatLock>( "QL" ),
          ElementaryDef.CreateLinear<ElementaryDot2>( "D2_U", ConstraintRowData.Def.U ),
          ElementaryDef.CreateLinear<ElementaryDot2>( "D2_V", ConstraintRowData.Def.V ),
          ElementaryDef.CreateLinear<RangeController>( "RT", ConstraintRowData.Def.N ),
          ElementaryDef.CreateLinear<TargetSpeedMotor>( "MT", ConstraintRowData.Def.N ),
          ElementaryDef.CreateLinear<LockController>( "LT", ConstraintRowData.Def.N )
        };
    }
  }

  /// <summary>
  /// Lock constraint.
  /// </summary>
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
  public class LockJoint : Constraint
  {
    protected LockJoint() { m_nativeType = typeof( agx.LockJoint ); }

    protected override List<ElementaryDef> GetElements()
    {
      return new List<ElementaryDef>
        {
          ElementaryDef.CreateLinear<ElementarySphericalRel>( "SR" ),
          ElementaryDef.CreateAngular<ElementaryQuatLock>( "QL" )
        };
    }
  }

  /// <summary>
  /// Cylindrical constraint with 2 x range, 2 x motor and 2 x lock.
  /// </summary>
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
  public class CylindricalJoint : Constraint
  {
    protected CylindricalJoint() { m_nativeType = typeof( agx.CylindricalJoint ); }

    protected override List<ElementaryDef> GetElements()
    {
      return new List<ElementaryDef>
        {
          ElementaryDef.CreateAngular<ElementaryDot1>( "D1_VN", ConstraintRowData.Def.U ),
          ElementaryDef.CreateAngular<ElementaryDot1>( "D1_UN", ConstraintRowData.Def.V ),
          ElementaryDef.CreateLinear<ElementaryDot2>( "D2_U", ConstraintRowData.Def.U ),
          ElementaryDef.CreateLinear<ElementaryDot2>( "D2_V", ConstraintRowData.Def.V ),
          ElementaryDef.CreateLinear<RangeController>( "RT", ConstraintRowData.Def.N ),
          ElementaryDef.CreateAngular<RangeController>( "RR", ConstraintRowData.Def.N ),
          ElementaryDef.CreateLinear<TargetSpeedMotor>( "MT", ConstraintRowData.Def.N ),
          ElementaryDef.CreateAngular<TargetSpeedMotor>( "MR", ConstraintRowData.Def.N ),
          ElementaryDef.CreateLinear<LockController>( "LT", ConstraintRowData.Def.N ),
          ElementaryDef.CreateAngular<LockController>( "LR", ConstraintRowData.Def.N )
        };
    }
  }

  /// <summary>
  /// Ball constraint.
  /// </summary>
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
  public class BallJoint : Constraint
  {
    protected BallJoint() { m_nativeType = typeof( agx.BallJoint ); }

    protected override List<ElementaryDef> GetElements()
    {
      return new List<ElementaryDef>
        {
          ElementaryDef.CreateLinear<ElementarySphericalRel>( "SR" )
        };
    }
  }

  /// <summary>
  /// Angular lock constraint.
  /// </summary>
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
  public class AngularLockJoint : Constraint
  {
    protected AngularLockJoint() { m_nativeType = typeof( agx.AngularLockJoint ); }

    protected override List<ElementaryDef> GetElements()
    {
      return new List<ElementaryDef>
        {
          ElementaryDef.CreateAngular<ElementaryQuatLock>( "QL" )
        };
    }
  }

  /// <summary>
  /// Distance constraint with range, motor and lock.
  /// Lock is enabled by default and the initial rest
  /// length is calculated in the PostCreate method.
  /// </summary>
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
  public class DistanceJoint : Constraint
  {
    protected DistanceJoint() { m_nativeType = typeof( agx.DistanceJoint ); }

    protected override List<ElementaryDef> GetElements()
    {
      return new List<ElementaryDef>
        {
          ElementaryDef.CreateLinear<RangeController>( "RT", ConstraintRowData.Def.N ),
          ElementaryDef.CreateLinear<TargetSpeedMotor>( "MT", ConstraintRowData.Def.N ),
          ElementaryDef.CreateLinear<LockController>( "LT", ConstraintRowData.Def.N )
        };
    }

    protected override void PostCreate()
    {
      base.PostCreate();

      ElementaryConstraint[] elementaryConstraints = GetComponents<ElementaryConstraint>();
      foreach ( ElementaryConstraint elementaryConstraint in elementaryConstraints ) {
        LockController lockController = elementaryConstraint as LockController;
        if ( lockController != null ) {
          lockController.Enable = true;
          lockController.Position = Vector3.Distance( m_frames[ 0 ].transform.position, m_frames[ 1 ].transform.position );
        }
      }
    }
  }

  /// <summary>
  /// Point in plane constraint.
  /// </summary>
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
  public class PlaneJoint : Constraint
  {
    protected PlaneJoint() { m_nativeType = typeof( agx.PlaneJoint ); }

    protected override List<ElementaryDef> GetElements()
    {
      return new List<ElementaryDef>
        {
          ElementaryDef.CreateLinear<ElementaryContactNormal>( "CN", ConstraintRowData.Def.N )
        };
    }
  }
}
