using System;
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
    
    public static float DefaultTimeStep { get { return Time.fixedDeltaTime; } }

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
    /// Display statistics window toggle.
    /// </summary>
    [SerializeField]
    private bool m_displayStatistics = false;

    /// <summary>
    /// Enable/disable statistics window showing timing and simulation data.
    /// </summary>
    public bool DisplayStatistics
    {
      get { return m_displayStatistics; }
      set
      {
        m_displayStatistics = value;

        if ( m_displayStatistics && m_statisticsWindowData == null )
          m_statisticsWindowData = new StatisticsWindowData( new Rect( new Vector2( 10, 10 ), new Vector2( 260, 175 ) ) );
        else if ( !m_displayStatistics && m_statisticsWindowData != null ) {
          m_statisticsWindowData.Dispose();
          m_statisticsWindowData = null;
        }
      }
    }

    [SerializeField]
    private bool m_enableMergeSplitHandler = false;
    public bool EnableMergeSplitHandler
    {
      get { return m_enableMergeSplitHandler; }
      set
      {
        m_enableMergeSplitHandler = value;
        if ( m_simulation != null )
          m_simulation.getMergeSplitHandler().setEnable( m_enableMergeSplitHandler );
      }
    }

    [SerializeField]
    private bool m_savePreFirstStep = false;
    [HideInInspector]
    public bool SavePreFirstStep
    {
      get { return m_savePreFirstStep; }
      set { m_savePreFirstStep = value; }
    }

    [SerializeField]
    private string m_savePreFirstStepPath = string.Empty;
    [HideInInspector]
    public string SavePreFirstStepPath
    {
      get { return m_savePreFirstStepPath; }
      set { m_savePreFirstStepPath = value; }
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

    /// <summary>
    /// Save current simulation/scene to an AGX native file (.agx or .aagx).
    /// </summary>
    /// <param name="filename">Filename including path.</param>
    /// <returns>True if objects were written to file - otherwise false.</returns>
    public bool SaveToNativeFile( string filename )
    {
      if ( m_simulation == null ) { 
        Debug.LogWarning( "Simulation isn't active - ignoring save scene to file: " + filename );
        return false;
      }

      uint numObjects = m_simulation.write( filename );
      return numObjects > 0;
    }

    protected override bool Initialize()
    {
      GetOrCreateSimulation();

      return base.Initialize();
    }

    protected void FixedUpdate()
    {
      if ( m_simulation != null ) {
        //if ( m_simulation.getTimeStamp() < 0.5f * TimeStep )
        //  OpenInAgXViewer();
        bool savePreFirstTimeStep = Application.isEditor &&
                                    SavePreFirstStep &&
                                    SavePreFirstStepPath != string.Empty &&
                                    m_simulation.getTimeStamp() == 0.0;
        if ( savePreFirstTimeStep ) {
          var saveSuccess = SaveToNativeFile( SavePreFirstStepPath );
          if ( saveSuccess )
            Debug.Log( "Successfully wrote initial state to: " + SavePreFirstStepPath );
        }

        StepCallbacks.PreStepForward?.Invoke();
        StepCallbacks.PreSynchronizeTransforms?.Invoke();
       
        m_simulation.stepForward();

        StepCallbacks.PostSynchronizeTransforms?.Invoke();
        StepCallbacks.PostStepForward?.Invoke();

        Rendering.DebugRenderManager.OnActiveSimulationPostStep( m_simulation );
      }
    }

    private class StatisticsWindowData : IDisposable
    {
      public int Id { get; private set; }
      public Rect Rect { get; set; }
      public Font Font { get; private set; }
      public GUIStyle LabelStyle { get; set; }

      public StatisticsWindowData( Rect rect )
      {
        agx.Statistics.instance().setEnable( true );
        Id = GUIUtility.GetControlID( FocusType.Passive );
        Rect = rect;

        var fonts = Font.GetOSInstalledFontNames();
        foreach ( var font in fonts )
          if ( font == "Consolas" )
            Font = Font.CreateDynamicFontFromOSFont( font, 12 );

        LabelStyle = Utils.GUI.Align( Utils.GUI.Skin.label, TextAnchor.MiddleLeft );
        if ( Font != null )
          LabelStyle.font = Font;
      }

      public void Dispose()
      {
        agx.Statistics.instance().setEnable( false );
      }
    }

    private StatisticsWindowData m_statisticsWindowData = null;

    protected void OnGUI()
    {
      if ( m_simulation == null || m_statisticsWindowData == null )
        return;

      var simColor      = Color.Lerp( Color.white, Color.blue, 0.2f );
      var spaceColor    = Color.Lerp( Color.white, Color.green, 0.2f );
      var dynamicsColor = Color.Lerp( Color.white, Color.yellow, 0.2f );
      var eventColor    = Color.Lerp( Color.white, Color.cyan, 0.2f );
      var dataColor     = Color.Lerp( Color.white, Color.magenta, 0.2f );

      var labelStyle = m_statisticsWindowData.LabelStyle;

      var simTime            = agx.Statistics.instance().getTimingInfo( "Simulation", "Step forward time" );
      var spaceTime          = agx.Statistics.instance().getTimingInfo( "Simulation", "Collision-detection time" );
      var dynamicsSystemTime = agx.Statistics.instance().getTimingInfo( "Simulation", "Dynamics-system time" );
      var preCollideTime     = agx.Statistics.instance().getTimingInfo( "Simulation", "Pre-collide event time" );
      var preTime            = agx.Statistics.instance().getTimingInfo( "Simulation", "Pre-step event time" );
      var postTime           = agx.Statistics.instance().getTimingInfo( "Simulation", "Post-step event time" );
      var lastTime           = agx.Statistics.instance().getTimingInfo( "Simulation", "Last-step event time" );

      var numBodies      = m_simulation.getDynamicsSystem().getRigidBodies().Count;
      var numShapes      = m_simulation.getSpace().getGeometries().Count;
      var numConstraints = m_simulation.getDynamicsSystem().getConstraints().Count +
                           m_simulation.getSpace().getGeometryContacts().Count;

      GUILayout.Window( m_statisticsWindowData.Id,
                        m_statisticsWindowData.Rect,
                        id =>
                        {
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "Total time:            ", simColor ) + simTime.current.ToString( "0.00" ) + " ms", 14, true ), labelStyle );
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "  - Pre-collide step:      ", eventColor ) + preCollideTime.current.ToString( "0.00" ) + " ms" ), labelStyle );
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "  - Collision detection:   ", spaceColor ) + spaceTime.current.ToString( "0.00" ) + " ms" ), labelStyle );
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "  - Pre step:              ", eventColor ) + preTime.current.ToString( "0.00" ) + " ms" ), labelStyle );
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "  - Dynamics solvers:      ", dynamicsColor ) + dynamicsSystemTime.current.ToString( "0.00" ) + " ms" ), labelStyle );
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "  - Post step:             ", eventColor ) + postTime.current.ToString( "0.00" ) + " ms" ), labelStyle );
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "  - Last step:             ", eventColor ) + lastTime.current.ToString( "0.00" ) + " ms" ), labelStyle );
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "Data:                  ", dataColor ), 14, true ), labelStyle );
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "  - Update frequency:      ", dataColor ) + (int)( 1.0f / TimeStep + 0.5f ) + " Hz" ), labelStyle );
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "  - Number of bodies:      ", dataColor ) + numBodies ), labelStyle );
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "  - Number of shapes:      ", dataColor ) + numShapes ), labelStyle );
                          GUILayout.Label( Utils.GUI.MakeLabel( Utils.GUI.AddColorTag( "  - Number of constraints: ", dataColor ) + numConstraints ), labelStyle );
                        },
                        "AGX Dynamics statistics",
                        Utils.GUI.Skin.window );
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
      if ( m_simulation == null ) {
        agx.Thread.registerAsAgxThread();
        m_simulation = new agxSDK.Simulation();
      }

      return m_simulation;
    }

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

      if ( !SaveToNativeFile( path + tmpFilename ) ) {
        Debug.Log( "Unable to start viewer.", this );
        return;
      }

      System.IO.File.WriteAllText( path + tmpLuaFilename, luaFileContent );

      System.Diagnostics.Process.Start( "luaagx.exe", path + tmpLuaFilename+ " -p --renderDebug 1" );
    }
  }
}
