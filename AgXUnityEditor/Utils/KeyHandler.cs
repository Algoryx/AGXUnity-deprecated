using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace AgXUnityEditor.Utils
{
  /// <summary>
  /// Handles when specific key is down/pressed during GUI event loop.
  /// </summary>
  public class KeyHandler
  {
    /// <summary>
    /// True if the given key is down - otherwise false.
    /// </summary>
    public bool IsDown
    {
      get { return m_isDown; }
      private set
      {
        if ( value && value != m_isDown && HideDefaultHandlesWhenIsDown ) {
          if ( m_defaultHandleStateHidden != null )
            Debug.LogError( "Default handle state already present." );

          m_defaultHandleStateHidden = new Tools.Tool.HideDefaultState();
        }
        else if ( !value ) {
          if ( m_defaultHandleStateHidden != null )
            m_defaultHandleStateHidden.OnRemove();
          m_defaultHandleStateHidden = null;
        }

        m_isDown = value;
      }
    }

    /// <summary>
    /// True to hide the default handles when the state is IsDown.
    /// </summary>
    public bool HideDefaultHandlesWhenIsDown { get; set; }

    /// <summary>
    ///  Default constructor.
    /// </summary>
    /// <param name="key">Key to handle.</param>
    public KeyHandler( params KeyCode[] keys )
    {
      m_keyData = ( from key in keys select new KeyData() { IsDown = false, Key = key } ).ToList();
      HideDefaultHandlesWhenIsDown = false;
    }

    public void OnRemove()
    {
      if ( m_defaultHandleStateHidden != null )
        m_defaultHandleStateHidden.OnRemove();
    }

    /// <summary>
    /// Update given current event. This method is automatically
    /// called during GUI update.
    /// </summary>
    public void Update( Event current )
    {
      bool allDown = m_keyData.Count > 0;
      foreach ( var data in m_keyData ) {
        if ( data.Key == KeyCode.LeftShift || data.Key == KeyCode.RightShift )
          data.IsDown = current.shift;
        else if ( current.type == EventType.KeyDown && data.Key == current.keyCode )
          data.IsDown = true;
        else if ( current.type == EventType.KeyUp && data.Key == current.keyCode )
          data.IsDown = false;

        allDown = allDown && data.IsDown;
      }

      IsDown = allDown;
    }

    private class KeyData
    {
      public KeyCode Key { get; set; }

      public bool IsDown { get; set; }
    }

    private bool m_isDown = false;
    private Tools.Tool.HideDefaultState m_defaultHandleStateHidden = null;
    private List<KeyData> m_keyData = new List<KeyData>();
  }
}
