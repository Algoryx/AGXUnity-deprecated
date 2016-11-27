﻿using System;
using AgXUnity.Utils;
using AgXUnity.Collide;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Rigid body object. Dynamic, kinematic or static, carrying mass and
  /// inertia. Possible to constrain and contains in general shapes.
  /// </summary>
  [AddComponentMenu( "AgXUnity/Rigid Body" )]
  [DisallowMultipleComponent]
  public class RigidBody : ScriptComponent
  {
    /// <summary>
    /// Native instance.
    /// </summary>
    private agx.RigidBody m_rb = null;

    #region Public Serialized Properties
    [SerializeField]
    private MassProperties m_massProperties = null;

    /// <summary>
    /// Mass properties of this rigid body.
    /// </summary>
    [HideInInspector]
    public MassProperties MassProperties
    {
      get
      {
        if ( m_massProperties == null ) {
          m_massProperties = MassProperties.Create<MassProperties>();
          m_massProperties.RigidBody = this;
        }

        return m_massProperties;
      }
    }

    /// <summary>
    /// Motion control of this rigid body, paired with property MotionControl.
    /// </summary>
    [SerializeField]
    private agx.RigidBody.MotionControl m_motionControl = agx.RigidBody.MotionControl.DYNAMICS;

    /// <summary>
    /// Get or set motion control of this rigid body.
    /// </summary>
    [System.ComponentModel.Description( "Change motion control:\n  - STATIC: Not moving, velocity and angular velocity ignored\n" +
                                        "  - KINEMATICS: Infinitely heavy, controlled with velocity and angular velocity\n" +
                                        "  - DYNAMICS: Moved given dynamics" )]
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
    [HideInInspector]
    public agx.RigidBody Native { get { return m_rb; } }

    /// <summary>
    /// True if the game object is active in hierarchy and this component is enabled.
    /// </summary>
    [HideInInspector]
    public bool IsEnabled { get { return gameObject.activeInHierarchy && enabled; } }
    #endregion

    #region Public Methods
    /// <summary>
    /// Updates mass properties script asset with current mass, inertia etc.
    /// </summary>
    public void UpdateMassProperties()
    {
      PeekTemporaryNativeOrGetNative( ( rb, isTemp ) =>
      {
        if ( !isTemp )
          rb.updateMassProperties();

        MassProperties.SetDefaultCalculated( rb );
      } );
    }

    /// <summary>
    /// Peek at a temporary native instance or the current (if initialized).
    /// </summary>
    /// <param name="callback">Callback with temporary or already initialized native instance. Callback signature ( nativeRb, isTemporary ).</param>
    /// <remarks>
    /// Always assume the native instance to be temporary. It's never safe to cache an instance to the native object.
    /// </remarks>
    public void PeekTemporaryNativeOrGetNative( Action<agx.RigidBody, bool> callback )
    {
      if ( callback == null )
        return;

      if ( m_rb != null )
        callback( m_rb, false );
      else {
        Shape[] shapes = GetComponentsInChildren<Shape>();

        using ( agx.RigidBody rb = new agx.RigidBody() ) {
          foreach ( Shape shape in shapes ) {
            agxCollide.Shape nativeShape = shape.CreateTemporaryNative();
            if ( nativeShape != null ) {
              agxCollide.Geometry geometry = new agxCollide.Geometry( nativeShape );

              geometry.setEnable( shape.IsEnabled );

              if ( shape.Material != null )
                geometry.setMaterial( shape.Material.CreateTemporaryNative() );
              rb.add( geometry, shape.GetNativeRigidBodyOffset( this ) );
            }
          }

          // For center of mass position/rotation to be correct we have to
          // synchronize the native transform given current game object transform.
          SyncNativeTransform( rb );

          callback( rb, true );

          // Hitting "Update" (mass or inertia in the Inspector) several times
          // will crash agx if we don't remove the geometries and shapes.
          while ( rb.getGeometries().Count > 0 ) {
            agxCollide.Geometry geometry = rb.getGeometries()[ 0 ].get();
            if ( geometry.getShapes().Count > 0 )
              geometry.remove( geometry.getShapes()[ 0 ].get() );
            rb.remove( geometry );
          }
        }
      }
    }
    #endregion

    #region Protected Virtual Methods
    protected override bool Initialize()
    {
      VerifyConfiguration();

      m_rb = new agx.RigidBody();
      m_rb.setName( name );
      m_rb.setEnable( IsEnabled );

      SyncNativeTransform( m_rb );

      SyncShapes();

      GetSimulation().add( m_rb );

      UpdateMassProperties();

      Simulation.Instance.StepCallbacks.PostSynchronizeTransforms += OnPostSynchronizeTransformsCallback;

      return base.Initialize();
    }

    protected override void OnEnable()
    {
      if ( Native != null && !Native.getEnable() )
        Native.setEnable( true );
    }

    protected override void OnDisable()
    {
      if ( Native != null && Native.getEnable() )
        Native.setEnable( false );
    }

    protected override void OnDestroy()
    {
      if ( GetSimulation() != null ) {
        Simulation.Instance.StepCallbacks.PostSynchronizeTransforms -= OnPostSynchronizeTransformsCallback;
        GetSimulation().remove( m_rb );
      }

      m_rb = null;

      base.OnDestroy();
    }

    private void OnPostSynchronizeTransformsCallback()
    {
      SyncUnityTransform();
      SyncProperties();

      Rendering.DebugRenderManager.OnPostSynchronizeTransforms( this );
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
          Debug.LogWarning( "Shape with name: " + shape.name + " failed to initialize. Ignored.", this );
          Debug.LogException( e, shape );
        }
      }
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
      // TODO: If "get" has native we can return the current velocity? Still possible to set.
      if ( m_rb == null )
        return;

      m_linearVelocity = m_rb.getVelocity().ToHandedVector3();
      m_angularVelocity = m_rb.getAngularVelocity().ToHandedVector3();
    }

    private void SyncNativeTransform( agx.RigidBody nativeRb )
    {
      if ( nativeRb == null )
        return;

      nativeRb.setPosition( transform.position.ToHandedVec3() );
      nativeRb.setRotation( transform.rotation.ToHandedQuat() );
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
