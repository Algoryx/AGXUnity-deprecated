using System;
using UnityEngine;


namespace AgXUnity.Rendering
{
  public static partial class Spawner
  {
    public static class Utils
    {
      public static float FindConstantScreenSizeScale( Vector3 position, Camera camera = null )
      {
        camera = camera ?? Camera.current ?? Camera.main;

        if ( camera != null ) {
          Transform transform = camera.transform;
          Vector3 position2   = transform.position;
          float z             = Vector3.Dot( position - position2, transform.TransformDirection( new Vector3( 0f, 0f, 1f ) ) );
          Vector3 a           = camera.WorldToScreenPoint( position2 + transform.TransformDirection( new Vector3( 0f, 0f, z ) ) );
          Vector3 b           = camera.WorldToScreenPoint( position2 + transform.TransformDirection( new Vector3( 1f, 0f, z ) ) );
          float magnitude     = ( a - b ).magnitude;

          return 80f / Mathf.Max( magnitude, 0.0001f );
        }

        return 20f;
      }

      public static void SetColor( GameObject gameObject, Color color )
      {
        if ( gameObject == null )
          return;

        var renderers = gameObject.GetComponentsInChildren<Renderer>();
        foreach ( var renderer in renderers )
          renderer.sharedMaterial.color = color;
      }

      public static float ConditionalConstantScreenSize( bool constantScreenSize, float size, Vector3 position )
      {
        return constantScreenSize ?
                 size * FindConstantScreenSizeScale( position ) :
                 size;                 
      }

      public static Vector3 ConditionalConstantScreenSize( bool constantScreenSize, Vector3 size, Vector3 position )
      {
        return constantScreenSize ?
                 Vector3.Scale( size, FindConstantScreenSizeScale( position ) * Vector3.one ) :
                 size;
      }

      public static void SetCylinderTransform( GameObject gameObject, Vector3 start, Vector3 end, float radius, bool constantScreenSize = false )
      {
        if ( gameObject == null )
          return;

        float r      = ConditionalConstantScreenSize( constantScreenSize, radius, 0.5f * ( start + end ) );
        Vector3 dir  = end - start;
        float height = dir.magnitude;
        dir         /= height;

        if ( height < 1.0E-4f )
          dir = Vector3.up;

        gameObject.transform.localScale = new Vector3( 2.0f * r, 0.5f * height, 2.0f * r );
        gameObject.transform.rotation   = Quaternion.FromToRotation( Vector3.up, dir );
        gameObject.transform.position   = 0.5f * ( start + end );
      }

      public static void SetSphereTransform( GameObject gameObject, Vector3 position, Quaternion rotation, float radius, bool constantScreenSize = false, float minRadius = 0f, float maxRadius = float.MaxValue )
      {
        if ( gameObject == null )
          return;

        gameObject.transform.localScale = 2.0f * Mathf.Clamp( ConditionalConstantScreenSize( constantScreenSize, radius, position ), minRadius, maxRadius ) * Vector3.one;
        gameObject.transform.rotation   = rotation;
        gameObject.transform.position   = position;
      }
    }
  }
}
