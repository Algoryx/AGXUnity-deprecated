using System;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity
{
  [AddComponentMenu( "" )]
  [RequireComponent( typeof( Rendering.CableRenderer ) )]
  [RequireComponent( typeof( CableRoute ) )]
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

    public enum RouteType
    {
      /// <summary>
      /// The added route nodes is the actual route.
      /// </summary>
      Identity,
      /// <summary>
      /// The route will try to fulfill the given route as good as possible
      /// given the resolution per unit length.
      /// </summary>
      Segmenting
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
          Native.setLinearVelocityDamping( new agx.Vec3( m_linearVelocityDamping, m_linearVelocityDamping, 0f ) );
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
          Native.setAngularVelocityDamping( new agx.Vec3( m_angularVelocityDamping, m_angularVelocityDamping, 0f ) );
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

    [SerializeField]
    private RouteType m_routeAlgorithm = RouteType.Segmenting;
    /// <summary>
    /// Route algorithm used by restored cables.
    /// </summary>
    [HideInInspector]
    public RouteType RouteAlgorithm
    {
      get { return m_routeAlgorithm; }
      set { m_routeAlgorithm = value; }
    }

    private CableRoute m_routeComponent = null;
    /// <summary>
    /// Get route to initialize this cable.
    /// </summary>
    public CableRoute Route
    {
      get
      {
        if ( m_routeComponent == null )
          m_routeComponent = GetComponent<CableRoute>();
        return m_routeComponent;
      }      
    }

    public void RestoreLocalDataFrom( agxCable.Cable native )
    {
      if ( native == null )
        return;

      Radius                  = Convert.ToSingle( native.getRadius() );
      ResolutionPerUnitLength = Convert.ToSingle( native.getResolution() );
      LinearVelocityDamping   = Convert.ToSingle( native.getLinearVelocityDamping().maxComponent() );
      AngularVelocityDamping  = Convert.ToSingle( native.getAngularVelocityDamping().maxComponent() );
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
      if ( ResolutionPerUnitLength < 1.0-6f ) {
        Debug.LogWarning( "Cable resolution is too low: " + ResolutionPerUnitLength + " segments per unit length. Ignoring cable.", this );
      }

      try {
        if ( Route.NumNodes < 2 )
          throw new Exception( "Invalid number of nodes. Minimum number of route nodes is two." );

        var cable = RouteAlgorithm == RouteType.Segmenting ?
                      new agxCable.Cable( Convert.ToDouble( Radius ), Convert.ToDouble( ResolutionPerUnitLength ) ) :
                      new agxCable.Cable( Convert.ToDouble( Radius ), new agxCable.IdentityRoute( ResolutionPerUnitLength ) );
        cable.addComponent( new agxCable.CablePlasticity() );
        cable.getCablePlasticity().setYieldPoint( double.PositiveInfinity, agxCable.Direction.ALL_DIRECTIONS );

        CableRouteNode prev = null;
        foreach ( var node in Route ) {
          bool tooClose = prev != null && Vector3.Distance( prev.Position, node.Position ) < 0.5f / ResolutionPerUnitLength;
          if ( !tooClose && !cable.add( node.GetInitialized<CableRouteNode>().Native ) )
            throw new Exception( "Unable to add node to cable." );
          if ( tooClose )
            Debug.LogWarning( "Ignoring route node with index: " + Route.IndexOf( node ) + ", since it's too close to its neighbor.", this );
          prev = node;
        }

        Native = cable;
      }
      catch ( Exception e ) {
        Debug.LogException( e, this );

        return false;
      }

      Native.setName( name );

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
        Native.getCableProperties().setDamping( Convert.ToDouble( Properties[ dir ].Damping ), CableProperties.ToNative( dir ) );
        Native.getCableProperties().setPoissonsRatio( Convert.ToDouble( Properties[ dir ].PoissonsRatio ), CableProperties.ToNative( dir ) );

        var plasticityComponent = Native.getCablePlasticity();
        if ( plasticityComponent != null )
          plasticityComponent.setYieldPoint( Convert.ToDouble( Properties[ dir ].YieldPoint ), CableProperties.ToNative( dir ) );
      }
    }
  }
}
