using System;
using System.Reflection;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Utils
{
  public static class CustomEditorGenerator
  {
    public static string Path { get { return Application.dataPath + @"/AgXUnity/Editor/CustomEditors/"; } }

    public static void Generate( Type type, bool refreshAssetDatabase = true )
    {
      GenerateEditor( type, Path );

      if ( refreshAssetDatabase )
        AssetDatabase.Refresh();
    }

    public static void Generate()
    {
      var types = GetAgXUnityTypes();
      foreach ( var type in types )
        Generate( type, false );

      AssetDatabase.Refresh();
    }

    public static Assembly GetAgXUnityAssembly()
    {
      return Assembly.Load( "AgXUnity" );
    }

    public static Type[] GetAgXUnityTypes()
    {
      var assembly = GetAgXUnityAssembly();
      if ( assembly == null ) {
        Debug.LogWarning( "Updating custom editors failed - unable to load AgXUnity.dll." );
        return new Type[] { };
      }

      return ( from type in assembly.GetTypes() where IsMatch( type ) select type ).ToArray();
    }

    public static void GenerateMissingEditors()
    {
      var types = GetAgXUnityTypes();

      bool assetDatabaseDirty = false;
      foreach ( var type in types ) {
        FileInfo info = new FileInfo( GetFilename( type, true ) );
        if ( !info.Exists ) {
          Debug.Log( "Custom editor for class " + type.ToString() + " is missing. Generating." );
          GenerateEditor( type, Path );
          assetDatabaseDirty = true;
        }
      }

      if ( assetDatabaseDirty )
        AssetDatabase.Refresh();
    }

    private static bool IsMatch( Type type )
    {
      return !type.IsAbstract &&
             !type.ContainsGenericParameters &&
            ( type.IsSubclassOf( typeof( ScriptComponent ) ) ||
              type.IsSubclassOf( typeof( ScriptAsset ) ) );
    }

    private static string GetClassName( Type type )
    {
      return type.ToString().Replace( ".", string.Empty );
    }

    private static string GetFilename( Type type, bool includePath )
    {
      string path = includePath ? Path : string.Empty;
      return path + GetClassName( type ) + "Editor.cs";
    }

    private static void GenerateEditor( Type type, string path )
    {
      string classAndFilename = GetClassName( type );
      string csFileContent = @"
using System;
using AgXUnity;
using AgXUnity.Collide;
using UnityEditor;

namespace AgXUnityEditor.Editors
{
  [CustomEditor( typeof( " + type.ToString() + @" ) )]
  public class " + classAndFilename + @"Editor : BaseEditor<" + type.ToString() + @">
  { }
}";
      File.WriteAllText( path + GetFilename( type, false ), csFileContent );
    }
  }
}
