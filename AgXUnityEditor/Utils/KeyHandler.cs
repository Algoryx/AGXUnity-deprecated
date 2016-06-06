using UnityEngine;

namespace AgXUnityEditor.Utils
{
  /// <summary>
  /// Handles when specific key is down/pressed during GUI event loop.
  /// </summary>
  public class KeyHandler
  {
    /// <summary>
    /// Key to check if pressed.
    /// </summary>
    public KeyCode Key { get; set; }

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
    public KeyHandler( KeyCode key )
    {
      Key = key;
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
      if ( Key == KeyCode.LeftShift || Key == KeyCode.RightShift )
        IsDown = current.shift;
      else if ( current.type == EventType.KeyDown && Key == current.keyCode )
        IsDown = true;
      else if ( current.type == EventType.KeyUp && Key == current.keyCode )
        IsDown = false;
    }

    private bool m_isDown = false;
    private Tools.Tool.HideDefaultState m_defaultHandleStateHidden = null;
  }
}
