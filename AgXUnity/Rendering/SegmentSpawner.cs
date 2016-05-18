using System;
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
    [SerializeField]
    private string m_prefabObjectPath = "";

    public SegmentSpawner( string prefabObjectPath )
    {
      m_prefabObjectPath = prefabObjectPath;
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
        m_segmentInstance = PrefabLoader.Instantiate( m_prefabObjectPath );
        m_segmentInstance.AddComponent<OnSelectionProxy>().Target = m_segments.transform.parent.gameObject;
        foreach ( Transform child in m_segmentInstance.transform )
          child.gameObject.AddComponent<OnSelectionProxy>().Target = m_segments.transform.parent.gameObject;
        Add( m_segmentInstance );
      }

      while ( m_counter >= m_segments.transform.childCount )
        Add( GameObject.Instantiate( m_segmentInstance ) as GameObject );

      return m_segments.transform.GetChild( m_counter++ ).gameObject;
    }
  }
}
