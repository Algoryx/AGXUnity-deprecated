using AgXUnity.Utils;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Simulation object, either explicitly created and added or
  /// implicitly created when first used.
  /// </summary>
  [GenerateCustomEditor]
  public class Simulation : UniqueGameObject<Simulation>
  {
    /// <summary>
    /// Native instance.
    /// </summary>
    private agxSDK.Simulation m_simulation = null;

    private double m_stepForwardTime = 0.0;

    /// <summary>
    /// Gravity, default -9.82 in y-direction. Paired with property Gravity.
    /// </summary>
    [SerializeField]
    Vector3 m_gravity = new Vector3( 0, -9.82f, 0 );

    /// <summary>
    /// Get or set gravity in this simulation. Default -9.82 in y-direction.
    /// </summary>
    public Vector3 Gravity
    {
      get { return m_gravity; }
      set
      {
        m_gravity = value;
        if ( m_simulation != null )
          m_simulation.setUniformGravity( m_gravity.AsVec3() );
      }
    }

    /// <summary>
    /// Time step size is the default callback frequency in Unity.
    /// </summary>
    [SerializeField]
    float m_timeStep = 1.0f / 50.0f;

    /// <summary>
    /// Get or set time step size. Note that the time step has to
    /// match Unity update frequency.
    /// </summary>
    public float TimeStep
    {
      get { return m_timeStep; }
      set
      {
        m_timeStep = value;
        if ( m_simulation != null )
          m_simulation.setTimeStep( m_timeStep );
      }
    }

    /// <summary>
    /// Get the native instance, if not deleted.
    /// </summary>
    public agxSDK.Simulation Native { get { return GetOrCreateSimulation(); } }

    protected override bool Initialize()
    {
      GetOrCreateSimulation();

      return base.Initialize();
    }

    protected void FixedUpdate()
    {
      if ( m_simulation != null ) {
        agx.Timer t = new agx.Timer( true );
        
        m_simulation.stepForward();

        t.stop();
        m_stepForwardTime = t.getTime();
      }
    }

    protected void OnGUI()
    {
      GUILayout.Label( "Step forward time: " + m_stepForwardTime + " ms." );
      GUILayout.Label( "Step forward FPS:  " + (int)( 1000.0 / m_stepForwardTime + 0.5 ) );
      GUILayout.Label( "Update frequencey: " + (int)( 1.0f / Time.fixedDeltaTime ) );
    }

    protected override void OnApplicationQuit()
    {
      base.OnApplicationQuit();
      if ( m_simulation != null )
        m_simulation.cleanup();
    }

    protected override void OnDestroy()
    {
      base.OnDestroy();
      if ( m_simulation != null )
        m_simulation.cleanup();
      m_simulation = null;
    }

    private agxSDK.Simulation GetOrCreateSimulation()
    {
      if ( m_simulation == null )
        m_simulation = new agxSDK.Simulation();

      return m_simulation;
    }
  }
}
