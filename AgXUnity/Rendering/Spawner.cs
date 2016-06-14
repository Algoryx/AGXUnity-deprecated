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
      Sphere,
      Constraint
    }

    public static GameObject Create( Primitive type, string name = "", HideFlags hideFlags = HideFlags.HideAndDontSave, string shaderName = "Diffuse" )
    {
      return Create( name, @"Debug/" + type.ToString() + "Renderer", hideFlags, shaderName );
    }

    public static GameObject CreateUnique( Primitive type, string name = "", HideFlags hideFlags = HideFlags.HideAndDontSave, string shaderName = "Diffuse" )
    {
      return CreateUnique( name, @"Debug/" + type.ToString() + "Renderer", hideFlags, shaderName );
    }

    public static void Destroy( GameObject gameObject )
    {
      if ( gameObject == null )
        return;

      MeshRenderer[] renderers = gameObject.GetComponentsInChildren<MeshRenderer>();
      foreach ( var renderer in renderers )
        GameObject.DestroyImmediate( renderer.sharedMaterial );

      GameObject.DestroyImmediate( gameObject );
    }

    private static GameObject Create( string name, string objPath, HideFlags hideFlags = HideFlags.HideInHierarchy, string shaderName = "Diffuse" )
    {
      GameObject gameObject = PrefabLoader.Instantiate<GameObject>( objPath );
      if ( gameObject == null )
        throw new AgXUnity.Exception( "Unable to load renderer: " + objPath );

      gameObject.name = name;

      gameObject.hideFlags = hideFlags;
      Shader shader = Shader.Find( shaderName ) ?? Shader.Find( "Diffuse" );
      if ( shader == null )
        throw new AgXUnity.Exception( "Enable to load shader: " + shaderName );

      MeshRenderer[] renderers = gameObject.GetComponentsInChildren<MeshRenderer>();
      foreach ( var renderer in renderers ) {
        renderer.sharedMaterial = new Material( shader );
        renderer.sharedMaterial.color = Color.yellow;
      }
      
      return gameObject;
    }

    private static GameObject CreateUnique( string name, string objPath, HideFlags hideFlags = HideFlags.HideAndDontSave, string shaderName = "Diffuse" )
    {
      GameObject shouldNotBeHere = GameObject.Find( name );
      if ( shouldNotBeHere != null )
        GameObject.DestroyImmediate( shouldNotBeHere );

      return Create( name, objPath, hideFlags, shaderName );
    }
  }
}
