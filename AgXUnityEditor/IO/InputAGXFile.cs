using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;
using Tree = AgXUnityEditor.IO.InputAGXFileTree;
using Node = AgXUnityEditor.IO.InputAGXFileTreeNode;

namespace AgXUnityEditor.IO
{
  public class InputAGXFile : IDisposable
  {
    public string Name { get; private set; }

    public string RelDirectoryPath { get; private set; }

    public FileInfo AGXFileInfo { get; private set; }

    public string PrefabFilename { get; private set; }

    public agxSDK.Simulation Simulation { get; private set; }

    public GameObject Parent { get; private set; }

    public InputAGXFile( FileInfo file )
    {
      if ( file == null )
        throw new ArgumentNullException( "file", "File info object is null." );

      if ( !file.Exists )
        throw new FileNotFoundException( "File not found: " + file.FullName );

      if ( file.Extension != ".agx" && file.Extension != ".aagx" )
        throw new AgXUnity.Exception( "Unsupported file format: " + file.Extension + " (file: " + file.FullName + ")" );

      AGXFileInfo      = file;
      Name             = Path.GetFileNameWithoutExtension( AGXFileInfo.Name );
      RelDirectoryPath = MakeRelative( AGXFileInfo.Directory.FullName, Application.dataPath ).Replace( '\\', '/' );
      PrefabFilename   = RelDirectoryPath + "/" + Name + ".prefab";

      Simulation = new agxSDK.Simulation();
    }

    public void TryLoad()
    {
      using ( new TimerBlock( "Loading: " + AGXFileInfo.Name ) )
        if ( !agxIO.agxIOSWIG.readFile( AGXFileInfo.FullName, Simulation ) )
          throw new AgXUnity.Exception( "Unable to load " + AGXFileInfo.Extension + " file: " + AGXFileInfo.FullName );
    }

    public void TryParse()
    {
      using ( new TimerBlock( "Parsing: " + AGXFileInfo.Name ) )
        m_tree.Parse( Simulation );
    }

    public void TryGenerate()
    {
      using ( new TimerBlock( "Generating: " + AGXFileInfo.Name ) ) {
        Parent = new GameObject( Name );
        Parent.transform.position = Vector3.zero;
        Parent.transform.rotation = Quaternion.identity;

        foreach ( var root in m_tree.Roots )
          Generate( root );
      }
    }

    public void Dispose()
    {
      if ( Simulation != null )
        Simulation.Dispose();
      Simulation = null;

      if ( Parent != null )
        GameObject.DestroyImmediate( Parent );
    }

    private void Generate( Node node )
    {
      if ( node == null )
        return;

      if ( node.GameObject == null ) {
        switch ( node.Type ) {
          case Node.NodeType.Assembly:
            agx.Frame frame = m_tree.GetAssembly( node.Uuid );
            node.GameObject = new GameObject( FindName( "", node.Type ) );
            Add( node );
            node.GameObject.transform.position = frame.getTranslate().ToHandedVector3();
            node.GameObject.transform.rotation = frame.getRotate().ToHandedQuaternion();
            break;
          case Node.NodeType.RigidBody:
            agx.RigidBody nativeRb = m_tree.GetRigidBody( node.Uuid );
            node.GameObject = new GameObject( FindName( nativeRb.getName(), node.Type ) );
            Add( node );
            node.GameObject.AddComponent<RigidBody>().RestoreLocalDataFrom( nativeRb );
            node.GameObject.transform.position = nativeRb.getPosition().ToHandedVector3();
            node.GameObject.transform.rotation = nativeRb.getRotation().ToHandedQuaternion();
            break;
          case Node.NodeType.Geometry:
            agxCollide.Geometry geometry = m_tree.GetGeometry( node.Uuid );
            node.GameObject = new GameObject( FindName( geometry.getName(), node.Type ) );
            Add( node );
            break;
        }
      }

      foreach ( var child in node.Children )
        Generate( child );
    }

    private void Add( Node node )
    {
      if ( node.GameObject == null ) {
        Debug.LogWarning( "Trying to add node with null GameObject." );
        return;
      }

      if ( node.Parent != null && node.Parent.GameObject == null ) {
        Debug.LogWarning( "Trying to add node with parent but parent hasn't been generated." );
        return;
      }

      if ( node.Parent != null )
        node.Parent.GameObject.AddChild( node.GameObject );
      else
        Parent.AddChild( node.GameObject );
    }

    private string FindName( string name, Node.NodeType type )
    {
      if ( name == "" )
        name = type.ToString();

      string result = name;
      int counter = 1;
      while ( m_names.Contains( result ) )
        result = name + " (" + ( counter++ ) + ")";

      m_names.Add( result );

      return result;
    }

    private string MakeRelative( string full, string root )
    {
      Uri fullUri = new Uri( full );
      Uri rootUri = new Uri( root );
      Uri relUri  = rootUri.MakeRelativeUri( fullUri );
      return relUri.ToString();
    }

    private Tree m_tree = new Tree();
    private HashSet<string> m_names = new HashSet<string>();
  }
}
