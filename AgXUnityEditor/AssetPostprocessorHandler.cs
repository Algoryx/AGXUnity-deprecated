using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;

namespace AgXUnityEditor
{
  public class AssetPostprocessorHandler : AssetPostprocessor
  {
    public static void OnPrefabAddedToScene( GameObject instance )
    {
      var prefab = PrefabUtility.GetPrefabParent( instance ) as GameObject;
      var prefabPath = AssetDatabase.GetAssetPath( prefab );
      var prefabInfo = new FileInfo( Application.dataPath + prefabPath.Remove( 0, "Assets".Length ) );
      var prefabDataPath = IO.InputAGXFile.MakeRelative( prefabInfo.DirectoryName, Application.dataPath )
                           .Replace( '\\', '/' ) + "/" + Path.GetFileNameWithoutExtension( prefabInfo.Name ) + "_Data";
      var contactMaterialsGuids = AssetDatabase.FindAssets( "t:AgXUnity.ContactMaterial", new string[]{ prefabDataPath } );
      foreach ( var guid in contactMaterialsGuids ) {
        var cm = AssetDatabase.LoadAssetAtPath<ContactMaterial>( AssetDatabase.GUIDToAssetPath( guid ) );
        if ( cm != null )
          ContactMaterialManager.Instance.Add( cm );
      }
    }

    private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
    {
      foreach ( var import in importedAssets ) {
        FileInfo info = new FileInfo( import );
        if ( info.Extension == ".agx" || info.Extension == ".aagx" )
          ReadAGXFile( info );
      }
    }

    private void OnPreprocessModel()
    {
    }

    private static void ReadAGXFile( FileInfo file )
    {
      try {
        using ( var inputFile = new IO.InputAGXFile( file ) ) {
          inputFile.TryLoad();
          inputFile.TryParse();
          inputFile.TryGenerate();
          inputFile.TryCreatePrefab();
        }
      }
      catch ( System.Exception e ) {
        Debug.LogException( e );
      }
    }
  }
}
