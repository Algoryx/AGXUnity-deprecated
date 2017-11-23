using System;
using System.IO;
using UnityEngine;
using Microsoft.Win32;

namespace AgXUnity
{
  /// <summary>
  /// Object handling initialize and shutdown of AgX.
  /// </summary>
  public class InitShutdownAgX
  {
    private agx.AutoInit m_ai = null;
    private EnvironmentVariableTarget m_envTarget = EnvironmentVariableTarget.Process;

    public bool Initialized { get; private set; }

    /// <summary>
    /// Default constructor - configuring AgX, making sure dll's are
    /// in path etc.
    /// </summary>
    public InitShutdownAgX()
    {
      Initialized = false;

      try {
        ConfigureAgX();
      }
      catch ( Exception e ) {
        Debug.LogException( e );
      }
    }

    ~InitShutdownAgX()
    {
      m_ai.Dispose();
      m_ai = null;
    }

    /// <summary>
    /// Binary path, when this module is part of a build, should be "." and
    /// therefore plugins in "./plugins", data in "./data" and license and
    /// other configuration files in "./cfg".
    /// </summary>
    public static string FindBinaryPath()
    {
      return ".";
    }

    public static string FindRuntimePathFromRegistry()
    {
      const string parent = "HKEY_LOCAL_MACHINE\\Software\\Wow6432Node\\Algoryx Simulation AB\\Algoryx\\AgX";
      return (string)Registry.GetValue( parent, "runtime", "" );
    }

    private void InitPath()
    {
    }

    private void ConfigureAgX()
    {
      string binaryPath = FindBinaryPath();

      // Check if agxDotNet.dll is in path.
      if ( !ExistInPath( "agxDotNet.dll" ) ) {
        // If it is not in path, lets look in the registry
        binaryPath = FindRuntimePathFromRegistry();

        // If no luck, then we need to bail out
        if ( binaryPath.Length == 0 )
          throw new AgXUnity.Exception( "Unable to find agxDotNet.dll - part of the AgX installation." );
        else
          AddToPath( binaryPath );
      }

      string pluginPath = binaryPath + @"\plugins";
      string dataPath = binaryPath + @"\data";
      string cfgPath = dataPath + @"\cfg";

      try {
        // Components are initialized in parallel and destroy is executed
        // from other worker threads. Enable local entity storages.
        agx.agxSWIG.setEntityCreationThreadSafe( true );

        m_ai = new agx.AutoInit();

        agx.agxSWIG.setNumThreads( 4 );

        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RUNTIME_PATH ).pushbackPath( binaryPath );
        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RUNTIME_PATH ).pushbackPath( pluginPath );

        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RESOURCE_PATH ).pushbackPath( binaryPath );
        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RESOURCE_PATH ).pushbackPath( pluginPath );
        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RESOURCE_PATH ).pushbackPath( dataPath );
        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RESOURCE_PATH ).pushbackPath( cfgPath );

        Initialized = true;
      }
      catch ( System.Exception e ) {
        throw new AgXUnity.Exception( "Unable to instantiate first AgX object. Some dependencies seems missing: " + e.ToString() );
      }
    }

    private bool ExistInPath( string filename )
    {
      if ( File.Exists( filename ) )
        return true;

      string path = Environment.GetEnvironmentVariable( "PATH", m_envTarget );
      foreach ( string p in path.Split( ';' ) ) {
        string fullPath = p + @"\" + filename;
        if ( File.Exists( fullPath ) )
          return true;
      }
      return false;
    }

    private void AddToPath( string path )
    {
      string currentPath = Environment.GetEnvironmentVariable( "PATH", m_envTarget );
      Environment.SetEnvironmentVariable( "PATH", currentPath + Path.PathSeparator + path, m_envTarget );
    }
  }

  /// <summary>
  /// Every ScriptComponent created will call "Register" and
  /// "Unregister" to this singleton. First call will initialize
  /// AgX and when this static instance is deleted AgX will be
  /// uninitialized.
  /// </summary>
  public class NativeHandler
  {
    #region Singleton Stuff
    private static NativeHandler m_instance = null;
    public static NativeHandler Instance
    {
      get
      {
        if ( m_instance == null )
          m_instance = new NativeHandler();
        return m_instance;
      }
    }
    #endregion

    private InitShutdownAgX m_isAgx = null;

    NativeHandler()
    {
      HasValidLicense = false;
      m_isAgx         = new InitShutdownAgX();

      if ( m_isAgx.Initialized && !agx.Runtime.instance().isValid() )
        Debug.LogError( "AGX Dynamics: " + agx.Runtime.instance().getStatus() );
      else if ( m_isAgx.Initialized )
        HasValidLicense = true;
    }

    ~NativeHandler()
    {
      m_isAgx = null;
    }

    public bool HasValidLicense { get; private set; }

    public bool Initialized { get { return m_isAgx != null && m_isAgx.Initialized; } }

    public void Register( ScriptComponent component )
    {
    }

    public void Unregister( ScriptComponent component )
    {
    }

    public void MakeMainThread()
    {
      if ( !agx.Thread.isMainThread() )
        agx.Thread.makeCurrentThreadMainThread();
    }

    public void RegisterCurrentThread()
    {
      if ( !agx.Thread.isMainThread() )
        agx.Thread.registerAsAgxThread();
    }
  }
}
