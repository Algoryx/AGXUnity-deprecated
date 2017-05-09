﻿using System;
using System.Linq;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity.Rendering
{
  [AddComponentMenu( "" )]
  [ExecuteInEditMode]
  public class CableRenderer : ScriptComponent
  {
    [SerializeField]
    private SegmentSpawner m_segmentSpawner = null;

    [HideInInspector]
    public SegmentSpawner SegmentSpawner { get { return m_segmentSpawner; } }

    [HideInInspector]
    public Cable Cable { get { return GetComponent<Cable>(); } }

    [SerializeField]
    private Material m_material = null;
    public Material Material
    {
      get { return m_material ?? m_segmentSpawner.DefaultMaterial; }
      set
      {
        m_material = value ?? m_segmentSpawner.DefaultMaterial;
        m_segmentSpawner.Material = m_material;
      }
    }

    protected override bool Initialize()
    {
      // Note that this is called in the editor as well [ExecuteInEditMode].
      if ( m_segmentSpawner != null ) {
        m_segmentSpawner.Destroy();
        m_segmentSpawner = null;
      }

      m_segmentSpawner = new SegmentSpawner( GetComponent<Cable>(), @"Cable/CableSegment", @"Cable/CableSegmentBegin" );
      m_segmentSpawner.Initialize( gameObject );

      // Use post step forward callback to render while simulating.
      if ( Application.isPlaying )
        Simulation.Instance.StepCallbacks.PostStepForward += Render;

      return true;
    }

    protected override void OnDestroy()
    {
      if ( m_segmentSpawner != null )
        m_segmentSpawner.Destroy();
      m_segmentSpawner = null;

      if ( Simulation.HasInstance )
        Simulation.Instance.StepCallbacks.PostStepForward -= Render;

      base.OnDestroy();
    }

    protected void LateUpdate()
    {
      // Late update from Editor. Exit if the application is running.
      if ( Application.isPlaying )
        return;

      Render( Cable.Route, Cable.Radius );
    }

    private void Render( CableRoute route, float radius )
    {
      if ( m_segmentSpawner == null )
        return;

      m_segmentSpawner.Begin();
      try {
        CableRouteNode[] nodes = route.ToArray();
        for ( int i = 1; i < nodes.Length; ++i )
          m_segmentSpawner.CreateSegment( nodes[ i - 1 ].Position, nodes[ i ].Position, radius );
      }
      catch ( System.Exception e ) {
        Debug.LogException( e, this );
      }
      m_segmentSpawner.End();
    }

    private void Render()
    {
      if ( m_segmentSpawner == null )
        return;

      agxCable.Cable native = Cable.Native;
      if ( native == null ) {
        if ( m_segmentSpawner != null ) {
          m_segmentSpawner.Destroy();
          m_segmentSpawner = null;
        }
        return;
      }

      m_segmentSpawner.Begin();
      try {
        agxCable.CableIterator it = native.begin();
        agxCable.CableIterator endIt = native.end();
        float radius = Cable.Radius;
        while ( !it.EqualWith( endIt ) ) {
          m_segmentSpawner.CreateSegment( it.getBeginPosition().ToHandedVector3(), it.getEndPosition().ToHandedVector3(), radius );
          it.inc();
        }
      }
      catch ( System.Exception e ) {
        Debug.LogException( e, this );
      }
      m_segmentSpawner.End();
    }
  }
}
