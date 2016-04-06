using UnityEngine;

namespace AgXUnity.Rendering
{
  public static class Spawner
  {
    public enum Primitive
    {
      Box,
      Capsule,
      Cylinder,
      Plane,
      Sphere
    }

    public static GameObject Create( Primitive type, string name = "", HideFlags hideFlags = HideFlags.HideInHierarchy, string shaderName = "Diffuse" )
    {
      return Create( name, "DebugRenderers." + type.ToString() + "Renderer", hideFlags, shaderName );
    }

    public static GameObject CreateUnique( Primitive type, string name = "", HideFlags hideFlags = HideFlags.HideInHierarchy, string shaderName = "Diffuse" )
    {
      return CreateUnique( name, "DebugRenderers." + type.ToString() + "Renderer", hideFlags, shaderName );
    }

    public static void Destroy( GameObject gameObject )
    {
      if ( gameObject == null )
        return;

      if ( gameObject.GetComponent<MeshRenderer>() != null )
        GameObject.DestroyImmediate( gameObject.GetComponent<MeshRenderer>().sharedMaterial );
      GameObject.DestroyImmediate( gameObject );
    }

    private static GameObject Create( string name, string objPath, HideFlags hideFlags = HideFlags.HideInHierarchy, string shaderName = "Diffuse" )
    {
      GameObject gameObject = PrefabLoader.Instantiate( objPath );
      if ( gameObject == null )
        throw new AgXUnity.Exception( "Unable to load renderer: " + objPath );

      gameObject.name = name;

      gameObject.hideFlags = hideFlags;
      Shader shader = Shader.Find( shaderName ) ?? Shader.Find( "Diffuse" );
      if ( shader == null )
        throw new AgXUnity.Exception( "Enable to load shader: " + shaderName );

      gameObject.GetComponent<MeshRenderer>().sharedMaterial = new Material( shader );
      gameObject.GetComponent<MeshRenderer>().sharedMaterial.color = Color.yellow;

      return gameObject;
    }

    private static GameObject CreateUnique( string name, string objPath, HideFlags hideFlags = HideFlags.HideInHierarchy, string shaderName = "Diffuse" )
    {
      GameObject shouldNotBeHere = GameObject.Find( name );
      if ( shouldNotBeHere != null )
        GameObject.DestroyImmediate( shouldNotBeHere );

      return Create( name, objPath, hideFlags, shaderName );
    }
  }
}
