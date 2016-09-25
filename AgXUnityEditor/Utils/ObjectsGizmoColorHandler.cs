using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Rendering;
using AgXUnity.Collide;

namespace AgXUnityEditor.Utils
{
  /// <summary>
  /// Specialized class for DrawGizmoCallbackHandler to manage the colors of objects.
  /// </summary>
  public class ObjectsGizmoColorHandler
  {
    private struct HSVDeltaData
    {
      public float DeltaHue;
      public float DeltaSaturation;
      public float DeltaValue;
      public float DeltaAlpha;

      public static HSVDeltaData HighlightRigidBody { get { return new HSVDeltaData() { DeltaHue = 0f, DeltaSaturation = -0.1f, DeltaValue = 0.15f, DeltaAlpha = 0f }; } }
      public static HSVDeltaData HighlightShape { get { return new HSVDeltaData() { DeltaHue = 0f, DeltaSaturation = -0.1f, DeltaValue = 0.35f, DeltaAlpha = 0.2f }; } }
      public static HSVDeltaData HighlightRigidBodyMeshFilter { get { return new HSVDeltaData() { DeltaHue = 0f, DeltaSaturation = -0.25f, DeltaValue = 0.15f, DeltaAlpha = 0f }; } }
    }

    private class RigidBodyColorData
    {
      public class ColorizedMeshFilterData
      {
        public Color Color;
      }

      public Color Color;
      public bool Colorized = false;
      public ColorizedMeshFilterData MeshFiltersData = null;
    }

    private Dictionary<MeshFilter, Color> m_meshColors           = new Dictionary<MeshFilter, Color>();
    private Dictionary<RigidBody, RigidBodyColorData> m_rbColors = new Dictionary<RigidBody, RigidBodyColorData>();
    private int m_oldRandomSeed                                  = Int32.MaxValue;

    public Color ShapeColor          = new Color( 0.05f, 0.85f, 0.15f, 0.15f );
    public Color MeshFilterColor     = new Color( 0.6f, 0.6f, 0.6f, 0.15f );
    public float RigidBodyColorAlpha = 0.15f;
    public int RandomSeed            = 1024;

    public Dictionary<MeshFilter, Color> ColoredMeshFilters { get { return m_meshColors; } }
    public TimeInterpolator01 TimeInterpolator { get; private set; }

    public ObjectsGizmoColorHandler()
    {
      TimeInterpolator = new TimeInterpolator01( 4f, 2f );
    }

    public Color GetOrCreateColor( RigidBody rb )
    {
      if ( rb == null )
        throw new ArgumentNullException( "rb" );

      return GetOrCreateColorData( rb ).Color;
    }

    public Color GetOrCreateColor( Shape shape )
    {
      if ( shape == null )
        throw new ArgumentNullException( "shape" );

      RigidBody rb = shape.GetComponentInParent<RigidBody>();
      if ( rb != null )
        return GetOrCreateColor( rb );

      return ShapeColor;
    }

    public Color Colorize( RigidBody rb )
    {
      if ( rb == null )
        throw new ArgumentNullException( "rb" );

      var colorData = GetOrCreateColorData( rb );
      if ( colorData.Colorized )
        return colorData.Color;

      var shapes = rb.GetComponentsInChildren<Shape>();
      foreach ( var shape in shapes ) {
        var shapeFilters = GetMeshFilters( shape );
        foreach ( var shapeFilter in shapeFilters )
          m_meshColors.Add( shapeFilter, colorData.Color );
      }

      colorData.Colorized = true;

      return colorData.Color;
    }

    public void ColorizeMeshFilters( RigidBody rb )
    {
      if ( rb == null )
        return;

      var colorData = GetOrCreateColorData( rb );
      if ( colorData.MeshFiltersData != null )
        return;

      var filters = rb.GetComponentsInChildren<MeshFilter>();
      Color filterColor = ChangeColorHSVDelta( colorData.Color, HSVDeltaData.HighlightRigidBodyMeshFilter );

      colorData.MeshFiltersData = new RigidBodyColorData.ColorizedMeshFilterData() { Color = filterColor };

      foreach ( var filter in filters )
        m_meshColors[ filter ] = filterColor;
    }

    public void Highlight( RigidBody rb )
    {
      if ( rb == null )
        return;

      Color rbColor = Colorize( rb );

      var shapes = rb.GetComponentsInChildren<Shape>();
      foreach ( var shape in shapes ) {
        var shapeFilters = GetMeshFilters( shape );
        foreach ( var shapeFilter in shapeFilters )
          m_meshColors[ shapeFilter ] = TimeInterpolator.Lerp( rbColor, ChangeColorHSVDelta( rbColor, HSVDeltaData.HighlightRigidBody ) );
      }

      var filters = rb.GetComponentsInChildren<MeshFilter>();
      foreach ( var filter in filters )
        m_meshColors[ filter ] = TimeInterpolator.Lerp( rbColor, ChangeColorHSVDelta( rbColor, HSVDeltaData.HighlightRigidBodyMeshFilter ) );
    }

    public void Highlight( Shape shape )
    {
      if ( shape == null )
        return;

      RigidBody rb = shape.GetComponentInParent<RigidBody>();
      Color color = rb != null ? Colorize( rb ) : ShapeColor;

      var shapeFilters = GetMeshFilters( shape );
      foreach ( var shapeFilter in shapeFilters )
        m_meshColors[ shapeFilter ] = TimeInterpolator.Lerp( color, ChangeColorHSVDelta( color, HSVDeltaData.HighlightShape ) );
    }

    public void Highlight( MeshFilter filter )
    {
      if ( filter == null )
        return;

      RigidBody rb = filter.GetComponentInParent<RigidBody>();
      if ( rb != null )
        Colorize( rb );

      Color color = MeshFilterColor;

      m_meshColors[ filter ] = TimeInterpolator.Lerp( color, ChangeColorHSVDelta( color, HSVDeltaData.HighlightRigidBodyMeshFilter ) );
    }

    public MeshFilter[] GetMeshFilters( Shape shape )
    {
      ShapeDebugRenderData shapeDebugRenderData = shape.GetComponent<ShapeDebugRenderData>();
      if ( shapeDebugRenderData != null && shapeDebugRenderData.Node != null )
        return shapeDebugRenderData.Node.GetComponentsInChildren<MeshFilter>();
      return new MeshFilter[] { };
    }

    public AgXUnity.Utils.DisposableCallback BeginEndScope()
    {
      Begin();
      return new AgXUnity.Utils.DisposableCallback( End );
    }

    public void Begin()
    {
      if ( m_oldRandomSeed != Int32.MaxValue || m_meshColors.Count > 0 || m_rbColors.Count > 0 ) {
        Debug.LogError( "Begin() called more than once before calling End()." );
        return;
      }

      m_oldRandomSeed = UnityEngine.Random.seed;
      UnityEngine.Random.seed = RandomSeed;
    }

    public void End()
    {
      m_meshColors.Clear();
      m_rbColors.Clear();

      if ( m_oldRandomSeed < Int32.MaxValue ) {
        UnityEngine.Random.seed = m_oldRandomSeed;
        m_oldRandomSeed = Int32.MaxValue;
      }
    }

    private RigidBodyColorData GetOrCreateColorData( RigidBody rb )
    {
      RigidBodyColorData colorData;
      if ( !m_rbColors.TryGetValue( rb, out colorData ) ) {
        Color color = new Color( UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value );
        //Color color = UnityEngine.Random.ColorHSV( 0f, 1f, 0.5f, 1.0f, 0.5f, 0.85f );

        color.a = RigidBodyColorAlpha;
        colorData = new RigidBodyColorData() { Color = color };

        m_rbColors.Add( rb, colorData );
      }

      return colorData;
    }

    /// <summary>
    /// Change color given delta in hue, saturation and value. All values are clamped between 0 and 1.
    ///   * Hue - change the actual color.
    ///   * Saturation - lowest value and the color is white, highest for clear color.
    ///   * Value - Brightness of the color, i.e., 0 for black and 1 for the actual color.
    /// </summary>
    private Color ChangeColorHSVDelta( Color color, HSVDeltaData data )
    {
      float h, s, v;
      Color.RGBToHSV( color, out h, out s, out v );

      v = Mathf.Clamp01( h + data.DeltaHue );
      // Decreasing saturation to make it more white.
      s = Mathf.Clamp01( s + data.DeltaSaturation );
      // Increasing value to make it more intense.
      v = Mathf.Clamp01( v + data.DeltaValue );

      Color newColor = Color.HSVToRGB( h, s, v );
      newColor.a     = Mathf.Clamp01( color.a + data.DeltaAlpha );
      return newColor;
    }
  }
}
