using System;
using System.Linq;
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
    private string m_prefabObjectPath = string.Empty;

    [SerializeField]
    private string m_separateFirstObjectPrefabPath = string.Empty;
    [SerializeField]
    private GameObject m_firstSegmentInstance = null;

    public SegmentSpawner( string prefabObjectPath, string separateFirstObjectPath = "" )
    {
      m_prefabObjectPath = prefabObjectPath;
      if ( separateFirstObjectPath != "" )
        m_separateFirstObjectPrefabPath = separateFirstObjectPath;
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
      return CreateSegment( start, end, 2f * radius, 2f * radius );
    }

    public GameObject CreateSegment( Vector3 start, Vector3 end, float width, float height )
    {
      Vector3 startToEnd = end - start;
      float length = startToEnd.magnitude;
      startToEnd /= length;
      if ( length < 0.0001f )
        return null;

      GameObject instance = GetInstance();

      if ( instance == m_firstSegmentInstance ) {
        Transform top    = instance.transform.GetChild( 0 );
        Transform main   = instance.transform.GetChild( 1 );
        Transform bottom = instance.transform.GetChild( 2 );

        main.localScale                    = new Vector3( width, length, height );
        top.localScale = bottom.localScale = new Vector3( width, width, height );
        top.transform.localPosition        =  0.5f * length * Vector3.up;
        bottom.transform.localPosition     = -0.5f * length * Vector3.up;
      }
      else {
        Transform main = instance.transform.GetChild( 0 );
        Transform top  = instance.transform.GetChild( 1 );

        main.localScale             = new Vector3( width, length, height );
        top.localScale              = new Vector3( width, width, height );
        top.transform.localPosition = new Vector3( 0, 0.5f * length, 0 );
      }

      instance.transform.rotation = Quaternion.FromToRotation( new Vector3( 0, 1, 0 ), startToEnd );
      instance.transform.position = start + 0.5f * length * startToEnd;

      return instance;
    }

    public void End()
    {
      DestroyFrom( m_counter );
    }

    private GameObject GetInstance()
    {
      if ( m_separateFirstObjectPrefabPath != string.Empty && m_firstSegmentInstance == null ) {
        m_firstSegmentInstance = PrefabLoader.Instantiate<GameObject>( m_separateFirstObjectPrefabPath );
        AddSelectionProxy( m_firstSegmentInstance );
        Add( m_firstSegmentInstance );
      }
      else if ( m_segmentInstance == null ) {
        m_segmentInstance = PrefabLoader.Instantiate<GameObject>( m_prefabObjectPath );
        AddSelectionProxy( m_segmentInstance );
        Add( m_segmentInstance );
      }

      // Push back new segment if there aren't enough segments already created.
      int currentChildCount = m_segments.transform.childCount;
      if ( m_counter == currentChildCount )
        Add( GameObject.Instantiate( m_segmentInstance ) );
      else if ( m_counter > currentChildCount )
        throw new Exception( "Internal counter is not in sync. Counter = " + m_counter + ", #children = " + currentChildCount );

      return m_segments.transform.GetChild( m_counter++ ).gameObject;
    }

    private void DestroyFrom( int index )
    {
      index = Mathf.Max( 0, index );

      while ( m_segments.transform.childCount > index )
        GameObject.DestroyImmediate( m_segments.transform.GetChild( m_segments.transform.childCount - 1 ).gameObject );
    }

    private void Add( GameObject child )
    {
      child.transform.parent = m_segments.transform;
    }

    private void AddSelectionProxy( GameObject instance )
    {
      instance.AddComponent<OnSelectionProxy>().Target = m_segments.transform.parent.gameObject;
      foreach ( Transform child in instance.transform )
        child.gameObject.AddComponent<OnSelectionProxy>().Target = m_segments.transform.parent.gameObject;
    }
  }
}
