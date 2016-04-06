using AgXUnity.Utils;
using UnityEngine;

namespace AgXUnity.Collide
{
  /// <summary>
  /// Height field object to be used with Unity "Terrain".
  /// </summary>
  [AddComponentMenu( "AgXUnity/Shapes/HeightField" )]
  [GenerateCustomEditor]
  public sealed class HeightField : Shape
  {
    /// <summary>
    /// Returns the native height field object if created.
    /// </summary>
    public agxCollide.HeightField Native { get { return m_shape as agxCollide.HeightField; } }

    /// <summary>
    /// Debug rendering scale and debug rendering in general not supported.
    /// </summary>
    public override UnityEngine.Vector3 GetScale()
    {
      return new Vector3( 1, 1, 1 );
    }

    /// <summary>
    /// Shape offset, rotates native height field from z up to y up, flips x and z (?) and
    /// moves to center of the terrain (Unity Terrain has origin "lower corner").
    /// </summary>
    /// <returns>Shape transform to be used between native geometry and shape.</returns>
    public override agx.AffineMatrix4x4 GetNativeGeometryOffset()
    {
      return agx.AffineMatrix4x4.rotate( agx.Vec3.Z_AXIS(),
                                         agx.Vec3.Y_AXIS() ).Multiply(
                                           agx.AffineMatrix4x4.rotate( -0.5f * Mathf.PI, agx.Vec3.Y_AXIS() ) ).Multiply(
                                             agx.AffineMatrix4x4.translate( 0.5f * GetWidth(), 0, 0.5f * GetHeight() ) );
    }

    /// <summary>
    /// Finds and returns the Unity Terrain object. Searches on this
    /// component level and in all parents.
    /// </summary>
    /// <returns>Unity Terrain object, if found.</returns>
    private UnityEngine.Terrain GetTerrain()
    {
      return Find.FirstParentWithComponent<Terrain>( transform );
    }

    /// <summary>
    /// Finds Unity Terrain data givet current setup.
    /// </summary>
    /// <returns>Unity TerrainData object, if found.</returns>
    private UnityEngine.TerrainData GetTerrainData()
    {
      Terrain terrain = GetTerrain();
      return terrain != null ? terrain.terrainData : null;
    }

    /// <returns>Width of the height field.</returns>
    private float GetWidth()
    {
      TerrainData data = GetTerrainData();
      return data != null ? data.size.x : 0.0f;
    }

    /// <returns>Global height reference.</returns>
    private float GetHeight()
    {
      TerrainData data = GetTerrainData();
      return data != null ? data.size.z : 0.0f;
    }

    /// <summary>
    /// Creates native height field object given current Unity Terrain
    /// object - if present (in component level or in parents).
    /// </summary>
    /// <returns>Native height field shape object.</returns>
    protected override agxCollide.Shape CreateNative()
    {
      TerrainData terrainData = GetTerrainData();
      if ( terrainData == null )
        return null;

      Vector3 scale = terrainData.heightmapScale;
      int[] res = new int[]{ terrainData.heightmapWidth, terrainData.heightmapHeight };
      float[,] heights = terrainData.GetHeights( 0, 0, res[ 0 ], res[ 1 ] );

      agxCollide.HeightField hf = new agxCollide.HeightField( (uint)res[ 0 ], (uint)res[ 1 ], GetWidth(), GetHeight(), 50.0 );
      for ( int y = 0; y < res[ 1 ]; ++y )
        for ( int x = 0; x < res[ 0 ]; ++x )
          hf.setHeight( (uint)x, (uint)y, heights[ x, y ] * scale.y );

      return hf;
    }
  }
}
