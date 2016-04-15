using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor
{
  public static class AssetsMenu
  {
    [MenuItem( "Assets/AgXUnity/Material" )]
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
  }
}
