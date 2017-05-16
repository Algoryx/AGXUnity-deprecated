using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor
{
  public class AssetPostprocessorHandler : AssetPostprocessor
  {
    public static UnityEngine.Object ReadAGXFile( string path )
    {
      return ReadAGXFile( new IO.AGXFileInfo( path ) );
    }

    public static UnityEngine.Object ReadAGXFile( IO.AGXFileInfo info )
    {
      if ( info == null || !info.IsValid )
        return null;

      try {
        UnityEngine.Object prefab = null;
        using ( var inputFile = new IO.InputAGXFile( info ) ) {
          inputFile.TryLoad();
          inputFile.TryParse();
          inputFile.TryGenerate();
          prefab = inputFile.TryCreatePrefab();
        }

        return prefab;
      }
      catch ( System.Exception e ) {
        Debug.LogException( e );
      }

      return null;
    }

    public static void OnPrefabAddedToScene( GameObject instance )
    {
      var fileInfo = new IO.AGXFileInfo( instance );
      if ( !fileInfo.IsValid || fileInfo.Type != IO.AGXFileInfo.FileType.AGXPrefab )
        return;

      if ( fileInfo.Parent == null ) {
        Debug.LogWarning( "Unable to load parent prefab from file: " + fileInfo.NameWithExtension );
        return;
      }

      foreach ( var cm in fileInfo.GetAssets<ContactMaterial>() )
        ContactMaterialManager.Instance.Add( cm );

      var fileData = fileInfo.Parent.GetComponent<AgXUnity.IO.RestoredAGXFile>();
      foreach ( var disabledGroup in fileData.DisabledGroups )
        CollisionGroupsManager.Instance.SetEnablePair( disabledGroup.First, disabledGroup.Second, false );
    }

    private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
    {
    }

    private void OnPreprocessModel()
    {
    }
  }
}
