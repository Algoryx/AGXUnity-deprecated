using System;
using UnityEngine;

namespace AgXUnity
{
  [AddComponentMenu( "" )]
  [RequireComponent( typeof( Rendering.CableRenderer ) )]
  public class Cable : ScriptComponent
  {
    /// <summary>
    /// Cable node types.
    /// </summary>
    public enum NodeType
    {
      BodyFixedNode,
      FreeNode
    }

    /// <summary>
    /// Native instance of the cable.
    /// </summary>
    public agxCable.Cable Native { get; private set; }

    /// <summary>
    /// Radius of this cable - default 0.05. Paired with property Radius.
    /// </summary>
    [SerializeField]
    private float m_radius = 0.05f;

    /// <summary>
    /// Get or set radius of this cable - default 0.05.
    /// </summary>
    [ClampAboveZeroInInspector]
    public float Radius
    {
      get { return m_radius; }
      set
      {
        m_radius = value;
      }
    }

    /// <summary>
    /// Convenience property for diameter of this cable.
    /// </summary>
    [ClampAboveZeroInInspector]
    public float Diameter
    {
      get { return 2.0f * Radius; }
      set { Radius = 0.5f * value; }
    }

    /// <summary>
    /// Resolution of this cable - default 5. Paired with property ResolutionPerUnitLength.
    /// </summary>
    [SerializeField]
    private float m_resolutionPerUnitLength = 5f;

    /// <summary>
    /// Get or set resolution of this cable. Default 5.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public float ResolutionPerUnitLength
    {
      get { return m_resolutionPerUnitLength; }
      set
      {
        m_resolutionPerUnitLength = value;
      }
    }

    /// <summary>
    /// Linear velocity damping of this cable.
    /// </summary>
    [SerializeField]
    private float m_linearVelocityDamping = 0.0f;

    /// <summary>
    /// Get or set linear velocity damping of this cable. Default 0.0.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public float LinearVelocityDamping
    {
      get { return m_linearVelocityDamping; }
      set
      {
        m_linearVelocityDamping = value;
        if ( Native != null )
          agxCable.Cable.setLinearVelocityDamping( Native.begin(), Native.end(), new agx.Vec3f( m_linearVelocityDamping, m_linearVelocityDamping, 0f ) );
      }
    }

    /// <summary>
    /// Angular velocity damping of this cable.
    /// </summary>
    [SerializeField]
    private float m_angularVelocityDamping = 0.0f;

    /// <summary>
    /// Get or set angular velocity damping of this cable. Default 0.0.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public float AngularVelocityDamping
    {
      get { return m_angularVelocityDamping; }
      set
      {
        m_angularVelocityDamping = value;
        if ( Native != null )
          agxCable.Cable.setAngularVelocityDamping( Native.begin(), Native.end(), new agx.Vec3f( m_angularVelocityDamping, m_angularVelocityDamping, 0f ) );
      }
    }

    /// <summary>
    /// Shape material of this wire. Default null.
    /// </summary>
    [SerializeField]
    private ShapeMaterial m_material = null;

    /// <summary>
    /// Get or set shape material of this wire. Default null.
    /// </summary>
    [AllowRecursiveEditing]
    public ShapeMaterial Material
    {
      get { return m_material; }
      set
      {
        m_material = value;
        if ( Native != null && m_material != null && m_material.Native != null )
          Native.setMaterial( m_material.Native );
      }
    }

    /// <summary>
    /// Cable properties.
    /// </summary>
    [SerializeField]
    private CableProperties m_properties = null;

    /// <summary>
    /// Get cable bulk properties instance.
    /// </summary>
    [AllowRecursiveEditing]
    [IgnoreSynchronization]
    public CableProperties Properties
    {
      get { return m_properties; }
      set
      {
        m_properties = value;
        SynchronizeProperties();
      }
    }

    /// <summary>
    /// Route to initialize this cable.
    /// </summary>
    [SerializeField]
    private CableRoute m_route = new CableRoute();

    /// <summary>
    /// Get route to initialize this cable.
    /// </summary>
    public CableRoute Route
    {
      get { return m_route; }
      set
      {
        m_route = value ?? new CableRoute();
      }
    }

    protected override void OnDestroy()
    {
      if ( GetSimulation() != null )
        GetSimulation().remove( Native );

      Native = null;

      base.OnDestroy();
    }

    protected override bool Initialize()
    {
      if ( m_route == null )
        return false;

      try {
        if ( Route.NumNodes < 2 )
          throw new Exception( "Invalid number of nodes. Minimum number of route nodes is two." );

        var cable = new agxCable.Cable( Convert.ToDouble( Radius ), Convert.ToDouble( ResolutionPerUnitLength ) );
        foreach ( var node in Route ) {
          if ( !cable.add( node.GetInitialized<CableRouteNode>().Native ) )
            throw new Exception( "Unable to add node to cable." );
        }

        Native = cable;
      }
      catch ( Exception e ) {
        Debug.LogException( e, this );

        return false;
      }

      GetSimulation().add( Native );

      SynchronizeProperties();

      return true;
    }

    private void SynchronizeProperties()
    {
      if ( Properties == null )
        return;

      if ( !Properties.IsListening( this ) )
        Properties.OnPropertyUpdated += OnPropertyValueUpdate;

      foreach ( CableProperties.Direction dir in CableProperties.Directions )
        OnPropertyValueUpdate( dir );
    }

    private void OnPropertyValueUpdate( CableProperties.Direction dir )
    {
      if ( Native != null ) {
        Native.getCableProperties().setYoungsModulus( Convert.ToDouble( Properties[ dir ].YoungsModulus ), CableProperties.ToNative( dir ) );
        Native.getCableProperties().setYieldPoint( Convert.ToDouble( Properties[ dir ].YieldPoint ), CableProperties.ToNative( dir ) );
        Native.getCableProperties().setDamping( Convert.ToDouble( Properties[ dir ].Damping ), CableProperties.ToNative( dir ) );
      }
    }
  }
}
