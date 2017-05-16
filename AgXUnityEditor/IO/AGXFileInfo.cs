using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AgXUnityEditor.IO
{
  public class AGXFileInfo
  {
    /// <summary>
    /// Supported file type.
    /// </summary>
    public enum FileType
    {
      Unknown,
      AGXBinary,
      AGXAscii,
      AGXPrefab
    }

    /// <summary>
    /// Makes relative path given complete path.
    /// </summary>
    /// <param name="complete">Complete path.</param>
    /// <param name="root">New root directory.</param>
    /// <returns>Path with <paramref name="root"/> as root.</returns>
    public static string MakeRelative( string complete, string root )
    {
      Uri completeUri = new Uri( complete );
      Uri rootUri = new Uri( root );
      Uri relUri = rootUri.MakeRelativeUri( completeUri );
      return relUri.ToString();
    }

    /// <summary>
    /// Returns true if file is an existing prefab with corresponding .agx/.aagx file.
    /// </summary>
    /// <param name="info">File info.</param>
    /// <returns>True if the .prefab file is has a corresponding .agx/.aagx file.</returns>
    public static bool IsExistingAGXPrefab( FileInfo info )
    {
      if ( info == null )
        return false;

      return info.Extension == ".prefab" &&
             info.Exists &&
             ( File.Exists( info.DirectoryName + "\\" + Path.GetFileNameWithoutExtension( info.Name ) + ".agx" ) ||
               File.Exists( info.DirectoryName + "\\" + Path.GetFileNameWithoutExtension( info.Name ) + ".aagx" ) );
    }

    /// <summary>
    /// Returns true if file is an existing AGX file.
    /// </summary>
    /// <param name="info">File info.</param>
    /// <returns>True if file exists and is an AGX file.</returns>
    public static bool IsExistingAGXFile( FileInfo info )
    {
      return info != null && info.Exists && ( info.Extension == ".agx" || info.Extension == ".aagx" );
    }

    /// <summary>
    /// Find file type given file path.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>File type.</returns>
    public static FileType FindType( string path )
    {
      return FindType( new FileInfo( path ) );
    }
    /// <summary>
    /// Find file type given file info.
    /// </summary>
    /// <param name="info">File info.</param>
    /// <returns>File type.</returns>

    public static FileType FindType( FileInfo info )
    {
      if ( info == null )
        return FileType.Unknown;

      return info.Extension == ".agx" ?
               FileType.AGXBinary :
             info.Extension == ".aagx" ?
               FileType.AGXAscii :
             IsExistingAGXPrefab( info ) ?
               FileType.AGXPrefab :
               FileType.Unknown;
    }
    
    /// <summary>
    /// True if valid path was given.
    /// </summary>
    public bool IsValid { get { return m_fileInfo != null; } }

    /// <summary>
    /// Name of the file - without extension.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Full name including absolute path.
    /// </summary>
    public string FullName { get { return m_fileInfo.FullName; } }

    /// <summary>
    /// Name of file including extension.
    /// </summary>
    public string NameWithExtension { get { return m_fileInfo.Name; } }

    /// <summary>
    /// Root directory - where the file is located.
    /// </summary>
    public string RootDirectory { get; private set; }

    /// <summary>
    /// File data directory path.
    /// </summary>
    public string DataDirectory { get; private set; }

    /// <summary>
    /// Prefab name including (relative) path.
    /// </summary>
    public string PrefabPath { get { return RootDirectory + "/" + Name + ".prefab"; } }

    /// <summary>
    /// File type.
    /// </summary>
    public FileType Type { get; private set; }

    /// <summary>
    /// True if file exists.
    /// </summary>
    public bool Exists { get { return m_fileInfo.Exists; } }

    /// <summary>
    /// Prefab parent if it exist.
    /// </summary>
    public GameObject Parent { get; private set; }

    /// <summary>
    /// Construct given path to file.
    /// </summary>
    /// <param name="path"></param>
    public AGXFileInfo( string path )
    {
      Construct( path );
    }

    /// <summary>
    /// Construct given prefab instance.
    /// </summary>
    /// <param name="prefabInstance"></param>
    public AGXFileInfo( GameObject prefabInstance )
    {
      Parent = PrefabUtility.GetPrefabParent( prefabInstance ) as GameObject;
      var prefabPath = AssetDatabase.GetAssetPath( Parent );

      Construct( prefabPath );
    }

    /// <summary>
    /// Creates data directory if it doesn't exists.
    /// </summary>
    /// <returns>Data directory info.</returns>
    public DirectoryInfo GetOrCreateDataDirectory()
    {
      if ( !Directory.Exists( DataDirectory ) )
        return Directory.CreateDirectory( DataDirectory );
      return new DirectoryInfo( DataDirectory );
    }

    /// <summary>
    /// Find asset path (in the data directory) given asset name.
    /// </summary>
    /// <param name="asset">Asset.</param>
    /// <returns>Path (relative) including .asset extension.</returns>
    public string GetAssetPath( UnityEngine.Object asset )
    {
      return DataDirectory + "/" + ( asset != null ? asset.name : "null" ) + ".asset";
    }

    /// <summary>
    /// Add an asset to in the data directory.
    /// </summary>
    /// <param name="asset">Asset to add.</param>
    public void AddAsset( UnityEngine.Object asset )
    {
      if ( asset == null ) {
        Debug.LogWarning( "Trying to add null asset to file: " + NameWithExtension );
        return;
      }

      AssetDatabase.CreateAsset( asset, GetAssetPath( asset ) );
    }

    /// <summary>
    /// Finds assets of given type in the data directory.
    /// </summary>
    /// <typeparam name="T">Asset type.</typeparam>
    /// <returns>Array of assets of given type in the data directory.</returns>
    public T[] GetAssets<T>()
      where T : UnityEngine.Object
    {
      var guids = AssetDatabase.FindAssets( "t:" + typeof( T ).FullName, new string[] { DataDirectory } );
      return ( from guid
               in guids
               select AssetDatabase.LoadAssetAtPath<T>( AssetDatabase.GUIDToAssetPath( guid ) ) ).ToArray();
    }

    /// <summary>
    /// Creates prefab given source game object and returns the prefab if successful.
    /// </summary>
    /// <param name="gameObject">Source game object.</param>
    /// <returns>Prefab if successful - otherwise null.</returns>
    public GameObject CreatePrefab( GameObject gameObject )
    {
      var prefab = PrefabUtility.CreateEmptyPrefab( PrefabPath );
      if ( prefab == null ) {
        Debug.LogWarning( "Unable to create prefab: " + PrefabPath );
        return null;
      }

      Parent = PrefabUtility.ReplacePrefab( gameObject, prefab );

      return Parent;
    }

    /// <summary>
    /// Saves assets and refreshes the database.
    /// </summary>
    public void Save()
    {
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
    }

    private void Construct( string path )
    {
      if ( path == "" )
        return;

      m_fileInfo    = new FileInfo( path );
      Name          = Path.GetFileNameWithoutExtension( m_fileInfo.Name );
      Type          = FindType( m_fileInfo );
      RootDirectory = MakeRelative( m_fileInfo.Directory.FullName, Application.dataPath ).Replace( '\\', '/' );
      DataDirectory = RootDirectory + "/" + Name + "_Data";
    }

    private FileInfo m_fileInfo = null;
  }
}
