using System;
using UnityEngine;
using UnityEditor;

namespace AgXUnityEditor.Utils
{
  public class TimeInterpolator01 : IDisposable
  {
    private double m_lastTime = 0.0;
    private bool m_goingUp    = true;

    public float Time { get; private set; }
    public float SpeedUp { get; set; }
    public float SpeedDown { get; set; }
    public float Velocity { get { return m_goingUp ? Mathf.Abs( SpeedUp ) : -Mathf.Abs( SpeedDown ); } }

    public TimeInterpolator01( float speedUp = 1f, float speedDown = 1f, float initialTime = 0f )
    {
      SpeedUp                   = speedUp;
      SpeedDown                 = speedDown;
      Time                      = Mathf.Clamp01( initialTime );
      EditorApplication.update += Update;
      m_lastTime                = EditorApplication.timeSinceStartup;
    }

    public void Dispose()
    {
      EditorApplication.update -= Update;
    }

    public void Reset( float initialTime = 0f )
    {
      Time       = Mathf.Clamp01( initialTime );
      m_lastTime = EditorApplication.timeSinceStartup;
    }

    public Color Lerp( Color min, Color max )
    {
      return Color.Lerp( min, max, Time );
    }

    private void Update()
    {
      float dt   = Convert.ToSingle( EditorApplication.timeSinceStartup - m_lastTime );
      m_lastTime = EditorApplication.timeSinceStartup;

      Time += Velocity * dt;

      m_goingUp = Time > 1f || Time < 0f ? !m_goingUp : m_goingUp;
      Time      = Mathf.Clamp01( Time );

      //m_t += Mathf.Max( Mathf.Abs( m_t ), 0.5f ) * m_v * dt;

      //if ( m_t >= 1f ) {
      //  m_t = 1f;
      //  m_v = -6f;
      //}
      //else if ( m_t <= 0f ) {
      //  m_t = 0f;
      //  m_v = 1.5f;
      //}
    }
  }
}
