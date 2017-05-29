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

      Undo.SetCurrentGroupName( "Adding: " + instance.name + " to scene." );
      var grouId = Undo.GetCurrentGroup();

      foreach ( var cm in fileInfo.GetAssets<ContactMaterial>() )
        TopMenu.GetOrCreateUniqueGameObject<ContactMaterialManager>().Add( cm );

      var fileData = fileInfo.Parent.GetComponent<AgXUnity.IO.RestoredAGXFile>();
      foreach ( var disabledGroup in fileData.DisabledGroups )
        TopMenu.GetOrCreateUniqueGameObject<CollisionGroupsManager>().SetEnablePair( disabledGroup.First, disabledGroup.Second, false );

      var renderDatas = instance.GetComponentsInChildren<AgXUnity.Rendering.ShapeVisual>();
      foreach ( var renderData in renderDatas ) {
        renderData.hideFlags |= HideFlags.NotEditable;
        renderData.transform.hideFlags |= HideFlags.NotEditable;
      }

      Undo.CollapseUndoOperations( grouId );
    }

    private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
    {
    }

    private void OnPreprocessModel()
    {
    }
  }
}
