using System.Linq;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Wire object.
  /// </summary>
  [AddComponentMenu( "" )]
  [RequireComponent( typeof( Rendering.WireRenderer ) )]
  [CustomTool( "AgXUnityEditor.Tools.WireTool" )]
  public class Wire : ScriptComponent
  {
    /// <summary>
    /// Route node, node types.
    /// </summary>
    public enum NodeType
    {
      BodyFixedNode,
      FreeNode,
      ConnectingNode,
      EyeNode,
      ContactNode,
      WinchNode
    }

    /// <summary>
    /// Get native instance, if initialized.
    /// </summary>
    public agxWire.Wire Native { get; private set; }

    /// <summary>
    /// Radius of this wire - default 0.015. Paired with property Radius.
    /// </summary>
    [SerializeField]
    private float m_radius = 0.015f;

    /// <summary>
    /// Get or set radius of this wire - default 0.015.
    /// </summary>
    [ClampAboveZeroInInspector]
    public float Radius
    {
      get { return m_radius; }
      set
      {
        m_radius = value;
        if ( Native != null )
          Native.setRadius( m_radius );
      }
    }

    /// <summary>
    /// Convenience property for diameter of this wire.
    /// </summary>
    [ClampAboveZeroInInspector]
    public float Diameter
    {
      get { return 2.0f * Radius; }
      set { Radius = 0.5f * value; }
    }

    /// <summary>
    /// Resolution of this wire - default 1.5. Paired with property ResolutionPerUnitLength.
    /// </summary>
    [SerializeField]
    private float m_resolutionPerUnitLength = 1.5f;

    /// <summary>
    /// Get or set resolution of this wire. Default 1.5.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public float ResolutionPerUnitLength
    {
      get { return m_resolutionPerUnitLength; }
      set
      {
        m_resolutionPerUnitLength = value;
        if ( Native != null )
          Native.setResolutionPerUnitLength( m_resolutionPerUnitLength );
      }
    }

    /// <summary>
    /// Linear velocity damping of this wire.
    /// </summary>
    [SerializeField]
    private float m_linearVelocityDamping = 0.0f;

    /// <summary>
    /// Get or set linear velocity damping of this wire. Default 0.0.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public float LinearVelocityDamping
    {
      get { return m_linearVelocityDamping; }
      set
      {
        m_linearVelocityDamping = value;
        if ( Native != null )
          Native.setLinearVelocityDamping( m_linearVelocityDamping );
      }
    }

    /// <summary>
    /// Internal. Scale constant of this wire - default 0.35. Paired with
    /// property ScaleConstant.
    /// </summary>
    [SerializeField]
    private float m_scaleConstant = 0.35f;

    /// <summary>
    /// Internal. Get or set scale constant of this wire. Default 0.35.
    /// </summary>
    [ClampAboveZeroInInspector]
    public float ScaleConstant
    {
      get { return m_scaleConstant; }
      set
      {
        m_scaleConstant = value;
        if ( Native != null )
          Native.getParameterController().setScaleConstant( m_scaleConstant );
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
    /// Route to initialize this wire.
    /// </summary>
    [SerializeField]
    private WireRoute m_route = new WireRoute();

    /// <summary>
    /// Get route to initialize this wire.
    /// </summary>
    public WireRoute Route
    {
      get { return m_route; }
      set
      {
        m_route = value ?? new WireRoute();
        m_route.Wire = this;
      }
    }

    /// <summary>
    /// Winch at begin of this wire if exists.
    /// </summary>
    [HideInInspector]
    public WireWinch BeginWinch
    {
      get { return Route.NumNodes > 0 ? Route.First().Winch : null; }
    }

    /// <summary>
    /// Winch at end of this wire if exists.
    /// </summary>
    [HideInInspector]
    public WireWinch EndWinch
    {
      get { return Route.NumNodes > 1 ? Route.Last().Winch : null; }
    }

    public Wire()
    {
      m_route.Wire = this;
    }

    protected override bool Initialize()
    {
      if ( m_route == null )
        return false;

      WireRoute.ValidatedRoute validatedRoute = Route.GetValidated();
      if ( !validatedRoute.Valid ) {
        Debug.LogError( validatedRoute.ErrorString, this );
        for ( int i = 0; i < validatedRoute.Nodes.Count; ++i )
          if ( !validatedRoute.Nodes[ i ].Valid )
            Debug.LogError( "[" + i + "]: " + validatedRoute.Nodes[ i ].ErrorString, this );

        return false;
      }

      try {
        Native = new agxWire.Wire( Radius, ResolutionPerUnitLength );
        Material = m_material != null ? m_material.GetInitialized<ShapeMaterial>() : null;
        int nodeCounter = 0;
        foreach ( WireRouteNode routeNode in m_route ) {
          agxWire.Node node = routeNode.GetInitialized<WireRouteNode>().Native;

          bool success = true;
          if ( node.getType() == agxWire.Node.Type.CONNECTING ) {
            // This is the first node, CM-node goes first.
            if ( nodeCounter == 0 ) {
              success = success && Native.add( node.getAsConnecting().getCmNode() );
              success = success && Native.add( node );
            }
            // This has to be the last node, CM-node goes last.
            else {
              success = success && Native.add( node );
              success = success && Native.add( node.getAsConnecting().getCmNode() );
            }
          }
          else if ( routeNode.Type == NodeType.WinchNode ) {
            if ( node == null )
              throw new AgXUnity.Exception( "Unable to initialize wire winch." );

            success = success && Native.add( routeNode.Winch.Native );
          }
          else
            success = success && Native.add( node );

          if ( !success )
            throw new AgXUnity.Exception( "Unable to add node " + nodeCounter + ": " + routeNode.Type );

          ++nodeCounter;
        }

        GetSimulation().add( Native );
        Simulation.Instance.StepCallbacks.PostStepForward += OnPostStepForward;
      }
      catch ( System.Exception e ) {
        Debug.LogException( e, this );
        return false;
      }

      return Native.initialized();
    }

    protected override void OnDestroy()
    {
      if ( GetSimulation() != null ) {
        GetSimulation().remove( Native );
        Simulation.Instance.StepCallbacks.PostStepForward -= OnPostStepForward;
      }

      Native = null;

      base.OnDestroy();
    }

    private void OnPostStepForward()
    {
      GetComponent<Rendering.WireRenderer>().OnPostStepForward( this );

      if ( BeginWinch != null )
        BeginWinch.OnPostStepForward( this );

      if ( EndWinch != null )
        EndWinch.OnPostStepForward( this );
    }
  }
}
