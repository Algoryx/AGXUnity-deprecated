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
    public bool IsDown { get; private set; }

    /// <summary>
    ///  Default constructor.
    /// </summary>
    /// <param name="key">Key to handle.</param>
    public KeyHandler( KeyCode key )
    {
      Key = key;
      IsDown = false;
      Manager.OnKeyHandlerConstruct( this );
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
  }
}
