using System.ComponentModel;
using AgXUnity.Utils;
using AgXUnity.Collide;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Components tailor made for the RigidBody class should inherit
  /// from this base class, receiving synchronize callbacks when
  /// needed.
  /// </summary>
  public abstract class RigidBodyComponent : ScriptComponent
  {
    /// <returns>Native instance if initialized - otherwise null.</returns>
    protected agx.RigidBody GetNative()
    {
      RigidBody rb = gameObject.GetComponent<RigidBody>();
      return rb != null ? rb.Native : null;
    }

    /// <summary>
    /// Synchronize callback when initialized.
    /// </summary>
    public virtual void Synchronize( RigidBody rb ) { }
  }

  /// <summary>
  /// Rigid body object. Dynamic, kinematic or static, carrying mass and
  /// inertia. Possible to constrain and contains in general shapes.
  /// </summary>
  [AddComponentMenu( "AgXUnity/Rigid Body" )]
  [RequireComponent( typeof( MassProperties ) )]
  [DisallowMultipleComponent]
  [GenerateCustomEditor]
  public class RigidBody : ScriptComponent
  {
    /// <summary>
    /// Method name, for the shapes added to this body, to use when
    /// e.g., their size has changed, affecting the mass and inertia.
    /// </summary>
    [HideInInspector]
    public static string UpdateMassMethodName = "UpdateMassProperties";

    /// <summary>
    /// Native instance.
    /// </summary>
    private agx.RigidBody m_rb = null;

    #region Public Serialized Properties
    /// <summary>
    /// Motion control of this rigid body, paired with property MotionControl.
    /// </summary>
    [SerializeField]
    private agx.RigidBody.MotionControl m_motionControl = agx.RigidBody.MotionControl.DYNAMICS;

    /// <summary>
    /// Get or set motion control of this rigid body.
    /// </summary>
    [Description("Change motion control:\n  - STATIC: Not moving, velocity and angular velocity ignored\n" +
                                         "  - KINEMATICS: Infinitely heavy, controlled with velocity and angular velocity\n" +
                                         "  - DYNAMICS: Moved given dynamics")]
    public agx.RigidBody.MotionControl MotionControl
    {
      get { return m_motionControl; }
      set
      {
        m_motionControl = value;

        if ( m_rb != null )
          m_rb.setMotionControl( value );
      }
    }

    /// <summary>
    /// Toggle if the rigid body should be handled as particle or not.
    /// Paired with property HandleAsParticle.
    /// </summary>
    [SerializeField]
    private bool m_handleAsParticle = false;

    /// <summary>
    /// Toggle if the rigid body should be handled as particle or not.
    /// If particle, the rotational degrees of freedoms are ignored.
    /// </summary>
    public bool HandleAsParticle
    {
      get { return m_handleAsParticle; }
      set
      {
        m_handleAsParticle = value;
        if ( Native != null )
          Native.setHandleAsParticle( m_handleAsParticle );
      }
    }

    /// <summary>
    /// Linear velocity of this rigid body, paired with property LinearVelocity.
    /// </summary>
    [SerializeField]
    private Vector3 m_linearVelocity = new Vector3();

    /// <summary>
    /// Get or set linear velocity of this rigid body.
    /// </summary>
    public Vector3 LinearVelocity
    {
      get { return m_linearVelocity; }
      set
      {
        m_linearVelocity = value;
        if ( Native != null )
          Native.setVelocity( m_linearVelocity.ToHandedVec3() );
      }
    }

    /// <summary>
    /// Angular velocity of this rigid body, paired with property AngularVelocity.
    /// </summary>
    [SerializeField]
    private Vector3 m_angularVelocity = new Vector3();

    /// <summary>
    /// Get or set angular velocity of this rigid body.
    /// </summary>
    public Vector3 AngularVelocity
    {
      get { return m_angularVelocity; }
      set
      {
        m_angularVelocity = value;
        if ( Native != null )
          Native.setAngularVelocity( m_angularVelocity.ToHandedVec3() );
      }
    }

    /// <summary>
    /// Linear velocity damping of this rigid body, paired with property LinearVelocityDamping.
    /// </summary>
    [SerializeField]
    private Vector3 m_linearVelocityDamping = new Vector3( 0, 0, 0 );

    /// <summary>
    /// Get or set linear velocity damping of this rigid body.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public Vector3 LinearVelocityDamping
    {
      get { return m_linearVelocityDamping; }
      set
      {
        m_linearVelocityDamping = value;
        if ( Native != null )
          Native.setLinearVelocityDamping( m_linearVelocityDamping.ToVec3f() );
      }
    }

    /// <summary>
    /// Angular velocity damping of this rigid body, paired with property AngularVelocityDamping.
    /// </summary>
    [SerializeField]
    private Vector3 m_angularVelocityDamping = new Vector3( 0, 0, 0 );

    /// <summary>
    /// Get or set angular velocity damping of this rigid body.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public Vector3 AngularVelocityDamping
    {
      get { return m_angularVelocityDamping; }
      set
      {
        m_angularVelocityDamping = value;
        if ( Native != null )
          Native.setAngularVelocityDamping( m_angularVelocityDamping.ToVec3f() );
      }
    }
    #endregion

    #region Public Properties
    /// <summary>
    /// Get native instance, if initialized.
    /// </summary>
    public agx.RigidBody Native { get { return m_rb; } }

    /// <summary>
    /// Magic property making it possible to assign shape materials
    /// through the inspector/editor.
    /// </summary>
    public ShapeMaterial AssignMaterialToAllShapes
    {
      get { return null; }
      set
      {
        Shape[] shapes = GetComponentsInChildren<Shape>();
        foreach ( Shape shape in shapes )
          shape.Material = value;
      }
    }
    #endregion

    /// <summary>
    /// Update mass properties given added shapes. This method
    /// has to be used if IgnoreOnChildAdded flag is set to true,
    /// for this rigid body to have the correct mass properties.
    /// </summary>
    public void ExplicitUpdateMassProperties()
    {
      UpdateMassProperties( null );
    }

    private bool m_ignoreOnChildAdded = false;

    /// <summary>
    /// When a shape is added to this body, mass properties given all shapes
    /// are calculated. If a tool adds a lot of shapes, set this field to
    /// true when the shapes are added and when done, invoke ExplicitUpdateMassProperties.
    /// </summary>
    [HideInInspector]
    public bool IgnoreOnChildAdded { get { return m_ignoreOnChildAdded; } set { m_ignoreOnChildAdded = value; } }

    /// <summary>
    /// Callback when the game object this component is part of receives
    /// a new child game object (e.g., a shape).
    /// </summary>
    /// <param name="child">Child game object.</param>
    public override void OnChildAdded( GameObject child )
    {
      if ( IgnoreOnChildAdded )
        return;

      base.OnChildAdded( child );

      UpdateMassProperties( null );
    }

    #region Protected Virtual Methods
    protected override bool Initialize()
    {
      VerifyConfiguration();

      m_rb = new agx.RigidBody();
      m_rb.setName( name );

      SyncNativeTransform();

      SyncShapes();

      GetComponent<MassProperties>().SetDefaultCalculated( m_rb );

      GetSimulation().add( m_rb );

      SyncComponents();

      m_rb.updateMassProperties();

      return base.Initialize();
    }

    protected override void OnDestroy()
    {
      if ( GetSimulation() != null )
        GetSimulation().remove( m_rb );

      m_rb = null;

      base.OnDestroy();
    }

    protected void LateUpdate()
    {
      SyncUnityTransform();
      SyncProperties();

      Rendering.DebugRenderManager.OnLateUpdate( this );
    }
    #endregion

    private void SyncShapes()
    {
      Shape[] shapes = GetComponentsInChildren<Shape>();
      foreach ( Shape shape in shapes ) {
        try {
          shape.GetInitialized<Shape>().SetRigidBody( this );
        }
        catch ( System.Exception e ) {
          Debug.LogWarning( "Shape with name: " + shape.name + " failed to initialize. Ignored." );
          Debug.LogException( e, shape );
        }
      }
    }

    private void SyncComponents()
    {
      RigidBodyComponent[] components = GetComponents<RigidBodyComponent>();
      foreach ( RigidBodyComponent component in components )
        component.Synchronize( this );
    }

    private void SyncUnityTransform()
    {
      if ( m_rb == null )
        return;

      // Local or global here? If we have a parent that moves?
      // If the parent moves, its transform has to be synced
      // down, and that is hard.
      transform.position = m_rb.getPosition().ToHandedVector3();
      transform.rotation = m_rb.getRotation().ToHandedQuaternion();
    }

    private void SyncProperties()
    {
      if ( m_rb == null )
        return;

      m_linearVelocity = m_rb.getVelocity().ToHandedVector3();
      m_angularVelocity = m_rb.getAngularVelocity().ToHandedVector3();
    }

    private void SyncNativeTransform()
    {
      if ( m_rb == null )
        return;

      m_rb.setPosition( transform.position.ToHandedVec3() );
      m_rb.setRotation( transform.rotation.ToHandedQuat() );
    }

    private void UpdateMassProperties( Shape sender )
    {
      if ( IgnoreOnChildAdded )
        return;

      // Not sure if possible to perform incremental update. For
      // now we'll ignore the sender.

      // If we don't have a mass properties component, we don't
      // have to perform this action. (Calculated or native
      // interface used.)
      MassProperties massProperties = GetComponent<MassProperties>();
      if ( massProperties == null )
        return;

      Shape[] shapes = GetComponentsInChildren<Shape>();
      if ( shapes.Length == 0 )
        return;

      // Fill temporary rigid body with geometries to calculate
      // mass and inertia.
      using ( agx.RigidBody rb = new agx.RigidBody() ) {
        foreach ( Shape shape in shapes ) {
          agxCollide.Shape nativeShape = shape.CreateTemporaryNative();
          if ( nativeShape != null ) {
            agxCollide.Geometry geometry = new agxCollide.Geometry( nativeShape );
            if ( shape.Material != null )
              geometry.setMaterial( shape.Material.CreateTemporaryNative() );
            rb.add( geometry, shape.GetNativeRigidBodyOffset( this ) );
          }
        }

        massProperties.SetDefaultCalculated( rb );

        while ( rb.getGeometries().Count > 0 ) {
          agxCollide.Geometry geometry = rb.getGeometries()[ 0 ].get();
          if ( geometry.getShapes().Count > 0 )
            geometry.remove( geometry.getShapes()[ 0 ].get() );
          rb.remove( geometry );
        }
      }
    }

    private void VerifyConfiguration()
    {
      // Verification:
      // - No parent may be a body.
      var parent = transform.parent;
      while ( parent != null ) {
        bool hasBody = parent.GetComponent<RigidBody>() != null;
        if ( hasBody )
          throw new Exception( "An AgXUnity.RigidBody may not have an other AgXUnity.RigidBody as parent." );
        parent = parent.parent;
      }
    }
  }
}
