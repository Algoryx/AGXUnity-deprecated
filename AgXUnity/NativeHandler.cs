using System;
using System.IO;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Object handling initialize and shutdown of AgX.
  /// </summary>
  public class InitShutdownAgX
  {
    private agx.AutoInit m_ai = null;
    private EnvironmentVariableTarget m_envTarget = EnvironmentVariableTarget.Process;

    /// <summary>
    /// Default constructor - configuring AgX, making sure dll's are
    /// in path etc.
    /// </summary>
    public InitShutdownAgX()
    {
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
    public static String FindBinaryPath()
    {
      return ".";
    }

    private void ConfigureAgX()
    {
      String binaryPath = FindBinaryPath();

      // Check if agxDotNet.dll is in path.
      if ( !ExistInPath( "agxDotNet.dll" ) )
        throw new AgXUnity.Exception( "Unable to find agxDotNet.dll - part of the AgX installation." );

      String pluginPath = binaryPath + @"\plugins";
      String dataPath = binaryPath + @"\data";
      String cfgPath = dataPath + @"\cfg";

      try {
        // Components are initialized in parallel and destroy is executed
        // from other worker threads. Enable local entity storages.
        agx.agxSWIG.setEntityCreationThreadSafe( true );

        m_ai = new agx.AutoInit();

        agx.Thread.registerAsAgxThread();

        agx.agxSWIG.setNumThreads( 4 );

        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RUNTIME_PATH ).pushbackPath( binaryPath );
        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RUNTIME_PATH ).pushbackPath( pluginPath );

        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RESOURCE_PATH ).pushbackPath( binaryPath );
        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RESOURCE_PATH ).pushbackPath( pluginPath );
        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RESOURCE_PATH ).pushbackPath( dataPath );
        agxIO.Environment.instance().getFilePath( agxIO.Environment.Type.RESOURCE_PATH ).pushbackPath( cfgPath );
      }
      catch ( System.Exception e ) {
        throw new AgXUnity.Exception( "Unable to instantiate first AgX object. Some dependencies seems missing: " + e.ToString() );
      }
    }

    private bool ExistInPath( String filename )
    {
      if ( System.IO.File.Exists( filename ) )
        return true;

      String path = System.Environment.GetEnvironmentVariable( "PATH", m_envTarget );
      foreach ( String p in path.Split( ';' ) ) {
        String fullPath = p + @"\" + filename;
        if ( System.IO.File.Exists( fullPath ) )
          return true;
      }
      return false;
    }

    private void AddToPath( String path )
    {
      String currentPath = System.Environment.GetEnvironmentVariable( "PATH", m_envTarget );
      System.Environment.SetEnvironmentVariable( "PATH", currentPath + Path.PathSeparator + path, m_envTarget );
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
      m_isAgx = new InitShutdownAgX() {};
    }

    ~NativeHandler()
    {
      m_isAgx = null;
    }

    public void Register( ScriptComponent component )
    {
    }

    public void Unregister( ScriptComponent component )
    {
    }
  }
}
