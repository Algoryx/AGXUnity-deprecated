using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor
{
  public static class AssetsMenu
  {
    [MenuItem( "Assets/AgXUnity/Shape Material" )]
    public static UnityEngine.Object CreateMaterial()
    {
      return Utils.AssetFactory.Create<ShapeMaterial>( "material" );
    }

    [MenuItem( "Assets/AgXUnity/Contact Material" )]
    public static UnityEngine.Object CreateContactMaterial()
    {
      return Utils.AssetFactory.Create<ContactMaterial>( "contact material" );
    }

    [MenuItem( "Assets/AgXUnity/Friction Model" )]
    public static UnityEngine.Object CreateFrictionModel()
    {
      return Utils.AssetFactory.Create<FrictionModel>( "friction model" );
    }

    [MenuItem( "Assets/AgXUnity/Cable Properties" )]
    public static UnityEngine.Object CreateCableProperties()
    {
      return Utils.AssetFactory.Create<CableProperties>( "cable properties" );
    }

    [MenuItem( "Assets/Import AGX file as prefab", validate = true )]
    public static bool IsAGXFileSelected()
    {
      bool agxFileFound = false;
      foreach ( var obj in Selection.objects ) {
        var assetPath = AssetDatabase.GetAssetPath( obj );
        agxFileFound = agxFileFound ||
                       IO.AGXFileInfo.IsExistingAGXFile( new System.IO.FileInfo( assetPath ) );
      }
      return agxFileFound;
    }

    [MenuItem( "Assets/Import AGX file as prefab" )]
    public static void GenerateAGXFileAsPrefab()
    {
      foreach ( var obj in Selection.objects ) {
        var info = new IO.AGXFileInfo( AssetDatabase.GetAssetPath( obj ) );
        if ( info.Type != IO.AGXFileInfo.FileType.AGXBinary && info.Type != IO.AGXFileInfo.FileType.AGXAscii )
          continue;

        AssetPostprocessorHandler.ReadAGXFile( info );
      }
    }

    private static bool IsAGXFile( string path )
    {
      var fi = new System.IO.FileInfo( path );
      return fi.Exists && ( fi.Extension == ".agx" || fi.Extension == ".aagx" );
    }
  }
}
