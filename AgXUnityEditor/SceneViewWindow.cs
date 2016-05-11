using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AgXUnityEditor
{
  /// <summary>
  /// Scene view window handled given a GUI callback method in any class.
  /// </summary>
  /// <example>
  /// public class MyEditorClass
  /// {
  ///   public void ShowWindow()
  ///   {
  ///     // Create window given RenderWindow method.
  ///     var data = SceneViewWindow.Show( RenderWindow, new Vector2( 300, 70 ), new Vector2( 300, 300 ), "My window" );
  ///     // Assign/change data for the window, e.g., if the user may drag the window.
  ///     data.Movable = true;
  ///   }
  ///   
  ///   public void CloseWindow()
  ///   {
  ///     // Close the window (if open), given our RenderWindow method.
  ///     SceneViewWindow.Close( RenderWindow );
  ///   }
  ///   
  ///   private void RenderWindow( EventType eventType )
  ///   {
  ///     GUILayout.Label( GUIHelper.MakeRTLabel( "This text is red.", Color.red ), GUIHelper.EditorSkin.label );
  ///     GUILayout.Label( GUIHelper.MakeRTLabel( "This test is blue.", Color.blue ), GUIHelper.EditorSkin.label );
  ///     GUILayout.Label( GUIHelper.MakeRTLabel( "Event type: " + eventType, Color.white ), GUIHelper.EditorSkin.label );
  ///   }
  /// }
  /// </example>
  public class SceneViewWindow
  {

    /// <summary>
    /// Window data object, holding callback, size, position etc. of the window.
    /// </summary>
    public class Data
    {
      private Rect m_rect = new Rect();

      /// <returns>Rect of the window.</returns>
      public Rect GetRect() { return m_rect; }

      /// <summary>
      /// Hack to compensate for Unity "window title" offset. Off by 20 pixels.
      /// </summary>
      /// <param name="position"></param>
      /// <returns></returns>
      public bool Contains( Vector2 position )
      {
        Rect rect = new Rect( m_rect );
        rect.position += new Vector2( 0, -20 );
        return rect.Contains( position );
      }

      /// <summary>
      /// GUI callback method: void MyCallback( EventType eventType ) where
      /// the eventType is the current event in the window.
      /// </summary>
      public Action<EventType> Callback { get; private set; }

      /// <summary>
      /// Size of the window.
      /// </summary>
      public Vector2 Size
      {
        get
        {
          return m_rect.size;
        }
        set
        {
          m_rect.size = value;
        }
      }

      /// <summary>
      /// Position of the window.
      /// </summary>
      public Vector2 Position
      {
        get
        {
          return m_rect.position;
        }
        set
        {
          value.x = (int)value.x;
          value.y = (int)value.y;

          m_rect.position = value;
        }
      }

      /// <summary>
      /// Title of the window.
      /// </summary>
      public string Title { get; set; }

      /// <summary>
      /// If true the window is possible to move. Default false.
      /// </summary>
      public bool Movable { get; set; }

      /// <summary>
      /// This flag is true if Movable == true and left mouse button is
      /// down in the window.
      /// </summary>
      public bool IsMoving { get; set; }

      /// <summary>
      /// Window id, automatically assigned.
      /// </summary>
      public int Id { get; private set; }

      /// <summary>
      /// Construct given callback.
      /// </summary>
      /// <param name="callack"></param>
      public Data( Action<EventType> callack )
      {
        Callback = callack;
        Id       = GUIUtility.GetControlID( FocusType.Passive );
        Movable  = false;
        IsMoving = false;
      }
    }

    private static Dictionary<Action<EventType>, Data> m_activeWindows = new Dictionary<Action<EventType>, Data>();

    /// <summary>
    /// Show a new window in scene view given callback (to render GUI), size, position and title.
    /// </summary>
    /// <param name="guiCallback">GUI callback.</param>
    /// <param name="size">Size of the window.</param>
    /// <param name="position">Position of the window.</param>
    /// <param name="title">Title of the window.</param>
    /// <returns>Window data.</returns>
    public static Data Show( Action<EventType> guiCallback, Vector2 size, Vector2 position, string title )
    {
      if ( guiCallback == null )
        throw new ArgumentNullException( "guiCallback" );

      Data data = null;
      if ( !m_activeWindows.TryGetValue( guiCallback, out data ) ) {
        data = new Data( guiCallback );
        m_activeWindows.Add( guiCallback, data );
      }

      data.Size     = size;
      data.Position = position;
      data.Title    = title;

      return data;
    }

    /// <summary>
    /// Close window given the GUI callback.
    /// </summary>
    /// <param name="guiCallback">GUI callback associated to the window.</param>
    public static void Close( Action<EventType> guiCallback )
    {
      m_activeWindows.Remove( guiCallback );
    }

    /// <summary>
    /// Finds window data for the given GUI callback.
    /// </summary>
    /// <param name="guiCallback">GUI callback associated to the window.</param>
    /// <returns>Window data.</returns>
    public static Data GetWindowData( Action<EventType> guiCallback )
    {
      Data data = null;
      m_activeWindows.TryGetValue( guiCallback, out data );
      return data;
    }

    public static void OnSceneView( SceneView sceneView )
    {
      foreach ( Data data in m_activeWindows.Values ) {
        Rect rect = GUILayout.Window( data.Id,
                                      data.GetRect(),
                                      id =>
                                      {
                                        // Call to the user method.
                                        EventType windowEventType = Event.current.GetTypeForControl( id );
                                        data.Callback( windowEventType );

                                        // Handle movable window.
                                        if ( data.Movable ) {
                                          // We'll have Repaint, Layout etc. events here as well and
                                          // GUI.DragWindow has to be called for all these other events.
                                          // Call DragWindow from mouse down to mouse up.
                                          data.IsMoving = windowEventType != EventType.MouseUp &&
                                                          ( data.IsMoving || windowEventType == EventType.MouseDown );

                                          if ( data.IsMoving )
                                            GUI.DragWindow();
                                        }
                                      },
                                      data.Title,
                                      Utils.GUIHelper.EditorSkin.window,
                                      new GUILayoutOption[] { GUILayout.Width( data.Size.x ) } );

        data.Size     = rect.size;
        data.Position = rect.position;
      }
    }
  }
}
