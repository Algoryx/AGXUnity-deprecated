using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AgXUnity.Rendering
{
  [AddComponentMenu( "" )]
  [RequireComponent( typeof( LineRenderer ) )]
  [ExecuteInEditMode]
  public class SimpleLineRenderer : ScriptComponent
  {
    public static SimpleLineRenderer Create()
    {
      GameObject gameObject = new GameObject( "SimpleLineRenderer" );
      SimpleLineRenderer renderer = gameObject.AddComponent<SimpleLineRenderer>();
      renderer.WidthStart = renderer.WidthEnd = 0.025f;
      renderer.ColorStart = renderer.ColorEnd = Color.white;
      LineRenderer lineRenderer = renderer.GetLineRenderer();
      lineRenderer.sharedMaterial = new Material( Shader.Find( "Unlit/Color" ) );
      return renderer;
    }

    private List<Vector3> m_points = new List<Vector3>();

    public LineRenderer GetLineRenderer() { return GetComponent<LineRenderer>(); }

    public List<Vector3> Points { get { return m_points; } }

    public void Add( Vector3 point ) { m_points.Add( point ); }

    public float WidthStart { get; set; }
    public float WidthEnd { get; set; }

    public Color ColorStart { get; set; }
    public Color ColorEnd { get; set; }

    protected void Update()
    {
      LineRenderer renderer = GetLineRenderer();
      renderer.enabled = m_points.Count >= 2;
      if ( !renderer.enabled )
        return;

      renderer.SetVertexCount( m_points.Count );
      renderer.SetWidth( WidthStart, WidthEnd );
      renderer.SetColors( ColorStart, ColorEnd );

      for ( int i = 0; i < m_points.Count; ++i )
        renderer.SetPosition( i, m_points[ i ] );
    }

    protected override void OnDestroy()
    {
      if ( Application.isEditor )
        GameObject.DestroyImmediate( GetLineRenderer().sharedMaterial );
      else
        GameObject.Destroy( GetLineRenderer().sharedMaterial );

      base.OnDestroy();
    }
  }
}
