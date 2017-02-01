using AgXUnity.Utils;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Simulation object, either explicitly created and added or
  /// implicitly created when first used.
  /// </summary>
  [AddComponentMenu( "" )]
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
          m_simulation.setUniformGravity( m_gravity.ToVec3() );
      }
    }

    /// <summary>
    /// Time step size is the default callback frequency in Unity.
    /// </summary>
    [SerializeField]
    private float m_timeStep = 1.0f / 50.0f;

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

    private StepCallbackFunctions m_stepCallbackFunctions = new StepCallbackFunctions();

    /// <summary>
    /// Step callback interface. Valid use from "initialize" to "Destroy".
    /// </summary>
    public StepCallbackFunctions StepCallbacks { get { return m_stepCallbackFunctions; } }

    protected override bool Initialize()
    {
      GetOrCreateSimulation();

      return base.Initialize();
    }

    protected void FixedUpdate()
    {
      if ( m_simulation != null ) {
        StepCallbacks.PreStepForward?.Invoke();
        StepCallbacks.PreSynchronizeTransforms?.Invoke();

        agx.Timer t = new agx.Timer( true );
        
        m_simulation.stepForward();

        t.stop();
        m_stepForwardTime = t.getTime();

        StepCallbacks.PostSynchronizeTransforms?.Invoke();
        StepCallbacks.PostStepForward?.Invoke();
      }
    }

    // TODO: Add scene view window (editor) with stats.
    //protected void OnGUI()
    //{
    //  GUILayout.Label( "Step forward time: " + m_stepForwardTime + " ms." );
    //  GUILayout.Label( "Step forward FPS:  " + (int)( 1000.0 / m_stepForwardTime + 0.5 ) );
    //  GUILayout.Label( "Update frequencey: " + (int)( 1.0f / Time.fixedDeltaTime ) );
    //}

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

    [InvokableInInspector( "Open in AgX native viewer", true )]
    public void OpenInAgXViewer()
    {
      if ( m_simulation == null ) {
        Debug.Log( "Unable to open simulation in native viewer.\nEditor has to be in play mode (or paused)." );
        return;
      }

      string path           = Application.dataPath + @"/AgXUnity/Resources/";
      string tmpFilename    = "openedInViewer.agx";
      string tmpLuaFilename = "openedInViewer.agxLua";

      var cameraData = new
      {
        Eye               = Camera.main.transform.position.ToHandedVec3().ToVector3(),
        Center            = ( Camera.main.transform.position + 25.0f * Camera.main.transform.forward ).ToHandedVec3().ToVector3(),
        Up                = Camera.main.transform.up.ToHandedVec3().ToVector3(),
        NearClippingPlane = Camera.main.nearClipPlane,
        FarClippingPlane  = Camera.main.farClipPlane,
        FOV               = Camera.main.fieldOfView
      };

      string luaFileContent = @"
assert( requestPlugin( ""agxOSG"" ) )
if not alreadyInitialized then
  alreadyInitialized = true
  local app = agxOSG.ExampleApplication()
  _G[ ""buildScene"" ] = function( sim, app, root )
                           assert( agxOSG.readFile( """ + path + tmpFilename + @""", sim, root ) )

                           local cameraData             = app:getCameraData()
                           cameraData.eye               = agx.Vec3( " + cameraData.Eye.x + ", " + cameraData.Eye.y + ", " + cameraData.Eye.z + @" )
                           cameraData.center            = agx.Vec3( " + cameraData.Center.x + ", " + cameraData.Center.y + ", " + cameraData.Center.z + @" )
                           cameraData.up                = agx.Vec3( " + cameraData.Up.x + ", " + cameraData.Up.y + ", " + cameraData.Up.z + @" )
                           cameraData.nearClippingPlane = " + cameraData.NearClippingPlane + @"
                           cameraData.farClippingPlane  = " + cameraData.FarClippingPlane + @"
                           cameraData.fieldOfView       = " + cameraData.FOV + @"
                           app:applyCameraData( cameraData )

                           return root
                         end
  app:addScene( arg[ 0 ], ""buildScene"", string.byte( ""1"" ) )
  local argParser = agxIO.ArgumentParser()
  argParser:readArguments( arg )
  if app:init( argParser ) then
    app:run()
  end
end";

      uint numObjects = m_simulation.write( path + tmpFilename );
      if ( numObjects == 0 ) {
        Debug.Log( "Unable to start viewer.", this );
        return;
      }

      System.IO.File.WriteAllText( path + tmpLuaFilename, luaFileContent );

      System.Diagnostics.Process.Start( "luaagx.exe", path + tmpLuaFilename+ " -p --renderDebug 1" );
    }
  }
}
