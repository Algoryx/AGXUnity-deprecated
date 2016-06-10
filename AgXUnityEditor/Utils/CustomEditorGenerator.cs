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
      Assembly assembly = Assembly.Load( "AgXUnity" );
      if ( assembly == null ) {
        Debug.LogWarning( "Updating custom editors failed - unable to load AgXUnity.dll." );
        return;
      }

      var classes = from type in assembly.GetTypes() where IsMatch( type ) select type;
      classes.ToList().ForEach( type => Generate( type, false ) );

      AssetDatabase.Refresh();
    }

    private static bool IsMatch( Type type )
    {
      return !type.IsAbstract &&
             !type.ContainsGenericParameters &&
            ( type.IsSubclassOf( typeof( ScriptComponent ) ) ||
              type.IsSubclassOf( typeof( ScriptAsset ) ) );
    }

    private static void GenerateEditor( Type type, string path )
    {
      string classAndFilename = type.ToString().Replace( ".", string.Empty );
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
      File.WriteAllText( path + classAndFilename + "Editor.cs", csFileContent );
    }
  }
}
