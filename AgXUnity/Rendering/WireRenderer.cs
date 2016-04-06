using System;
using System.Collections.Generic;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity.Rendering
{
  [Serializable]
  public class SegmentSpawner
  {
    [SerializeField]
    private GameObject m_segmentInstance = null;
    [SerializeField]
    private GameObject m_segments = null;
    [SerializeField]
    private int m_counter = 0;

    public string PrefabObject = "Wire.WireSegment";

    private void DestroyFrom( int index )
    {
      if ( index < 0 )
        index = 0;

      if ( m_segments.transform.childCount > index ) {
        Transform[] children = new Transform[ m_segments.transform.childCount - index ];
        for ( int i = index; i < m_segments.transform.childCount; ++i )
          children[ i - index ] = m_segments.transform.GetChild( i );
        foreach ( Transform child in children )
          GameObject.DestroyImmediate( child.gameObject );
      }
    }

    private void Add( GameObject child )
    {
      child.transform.parent = m_segments.transform;
    }

    private GameObject GetInstance()
    {
      if ( m_segmentInstance == null ) {
        m_segmentInstance = PrefabLoader.Instantiate( PrefabObject );
        Add( m_segmentInstance );
      }

      while ( m_counter >= m_segments.transform.childCount )
        Add( GameObject.Instantiate( m_segmentInstance ) as GameObject );

      return m_segments.transform.GetChild( m_counter++ ).gameObject;
    }

    public void Initialize( GameObject parent )
    {
      if ( m_segments != null )
        return;

      m_segments = new GameObject( "RenderSegments" );
      parent.AddChild( m_segments, false );
    }

    public void Destroy()
    {
      if ( m_segments == null )
        return;

      m_segments.transform.parent = null;
      DestroyFrom( 0 );
      GameObject.DestroyImmediate( m_segmentInstance );
      GameObject.DestroyImmediate( m_segments );
      m_segmentInstance = null;
      m_segments = null;
    }

    public void Begin()
    {
      m_counter = 0;
    }

    public GameObject CreateSegment( Vector3 start, Vector3 end, float radius )
    {
      return CreateSegment( start, end, radius, radius );
    }

    public GameObject CreateSegment( Vector3 start, Vector3 end, float width, float height )
    {
      Vector3 startToEnd = end - start;
      float length = startToEnd.magnitude;
      startToEnd /= length;
      if ( length < 0.0001f )
        return null;

      GameObject instance = GetInstance();

      Transform main = instance.transform.GetChild( 0 );
      Transform top = instance.transform.GetChild( 1 );

      main.localScale = new Vector3( width, 0.5f * length, height );
      top.localScale = new Vector3( width, width, height );
      top.transform.localPosition = new Vector3( 0, 0.5f * length, 0 );
      instance.transform.rotation = Quaternion.FromToRotation( new Vector3( 0, 1, 0 ), startToEnd );
      instance.transform.position = start + 0.5f * length * startToEnd;

      return instance;
    }

    public void End()
    {
      DestroyFrom( m_counter );
    }
  }

  [GenerateCustomEditor]
  [ExecuteInEditMode]
  public class WireRenderer : ScriptComponent
  {
    [SerializeField]
    private SegmentSpawner m_segmentSpawner = null;

    public float NumberOfSegmentsPerMeter = 2.0f;

    protected override bool Initialize()
    {
      InitializeRenderer( true );

      return base.Initialize();
    }

    protected override void OnDestroy()
    {
      if ( m_segmentSpawner != null )
        m_segmentSpawner.Destroy();
      m_segmentSpawner = null;

      base.OnDestroy();
    }

    protected void LateUpdate()
    {
      Wire wire = GetComponent<Wire>();
      if ( wire == null )
        return;

      if ( wire.Native == null )
        RenderRoute( wire.Route.Nodes, wire.Radius );
      else
        Render( wire );
    }

    private void RenderRoute( List<Wire.RouteNode> route, float radius )
    {
      if ( route == null || route.Count < 2 )
        return;

      m_segmentSpawner.Begin();
      try {
        for ( int i = 1; i < route.Count; ++i )
          m_segmentSpawner.CreateSegment( route[ i - 1 ].WorldPosition, route[ i ].WorldPosition, radius );
      }
      catch ( System.Exception e ) {
        Debug.LogException( e );
      }
      m_segmentSpawner.End();
    }

    private void InitializeRenderer( bool destructLast = false )
    {
      if ( destructLast && m_segmentSpawner != null ) {
        m_segmentSpawner.Destroy();
        m_segmentSpawner = null;
      }

      m_segmentSpawner = new SegmentSpawner();
      m_segmentSpawner.Initialize( gameObject );
    }

    private void Render( Wire wire )
    {
      if ( m_segmentSpawner == null || wire == null || wire.Native == null || wire.Native.getRenderListEmpty() ) {
        if ( m_segmentSpawner != null )
          m_segmentSpawner.Destroy();
        m_segmentSpawner = null;
        return;
      }

      List<Vector3> positions      = new List<Vector3>();
      positions.Capacity           = 256;
      agxWire.RenderIterator it    = wire.Native.getRenderBeginIterator();
      agxWire.RenderIterator endIt = wire.Native.getRenderEndIterator();
      while ( !it.EqualWith( endIt ) ) {
        positions.Add( it.get().getWorldPosition().AsVector3() );
        it.inc();
      }

      m_segmentSpawner.Begin();
      try {
        for ( int i = 0; i < positions.Count - 1; ++i ) {
          Vector3 curr       = positions[ i ];
          Vector3 next       = positions[ i + 1 ];
          Vector3 currToNext = next - curr;
          float distance     = currToNext.magnitude;
          currToNext        /= distance;
          int numSegments    = Convert.ToInt32( distance * NumberOfSegmentsPerMeter + 0.5f );
          float dl           = distance / numSegments;
          for ( int j = 0; j < numSegments; ++j ) {
            next = curr + dl * currToNext;

            m_segmentSpawner.CreateSegment( curr, next, wire.Radius );
            curr = next;
          }
        }
      }
      catch ( System.Exception e ) {
        Debug.LogException( e );
      }

      m_segmentSpawner.End();
    }
  }
}
