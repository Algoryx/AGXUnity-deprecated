using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AgXUnity.Utils;
using UnityEngine;
using UnityEditor;

namespace AgXUnityEditor.Utils
{
  public static class GUIHelper
  {
    public static string AddColorTag( string str, Color color )
    {
      return @"<color=" + color.ToHexStringRGBA() + @">" + str + @"</color>";
    }

    public static GUIContent MakeLabel( string text, bool bold = false )
    {
      GUIContent label = new GUIContent();
      string boldBegin = bold ? "<b>" : "";
      string boldEnd   = bold ? "</b>" : "";
      label.text       = boldBegin + text + boldEnd;
      return label;
    }

    public static GUIContent MakeLabel( string text, Color color, bool bold = false )
    {
      GUIContent label = MakeLabel( text, bold );
      label.text       = AddColorTag( text, color );
      return label;
    }

    public static GUIContent MakeLabel( string text, Color color, int size, bool bold = false )
    {
      GUIContent label = MakeLabel( text, color, bold );
      label.text = @"<size=" + size + @">" + label.text + @"</size>";
      return label;
    }

    public static Vector3 Vector3Field( GUIContent content, Vector3 value )
    {
      EditorGUILayout.BeginHorizontal();
      GUILayout.Label( content );
      value = EditorGUILayout.Vector3Field( "", value );
      EditorGUILayout.EndHorizontal();

      return value;
    }

    public static GUIStyle EditorSkinLabel( TextAnchor alignment = TextAnchor.MiddleCenter )
    {
      GUIStyle style = new GUIStyle( EditorSkin.label );
      style.alignment = alignment;
      return style;
    }

    private static GUISkin m_editorGUISkin = null;
    public static GUISkin EditorSkin
    {
      get
      {
        if ( m_editorGUISkin == null )
          m_editorGUISkin = Resources.Load<GUISkin>( "AgXEditorGUISkin" );
        return m_editorGUISkin ?? GUI.skin;
      }
    }

    public static void Separator( float height = 1.0f, float space = 2.0f )
    {
      Texture2D lineTexture = new Texture2D( 1, 1, TextureFormat.RGBA32, true );

      if ( EditorGUIUtility.isProSkin )
        lineTexture.SetPixel( 0, 1, Color.white );
      else
        lineTexture.SetPixel( 0, 1, Color.black );

      lineTexture.Apply();

      GUILayout.Space( space );
      EditorGUI.DrawPreviewTexture( EditorGUILayout.GetControlRect( new GUILayoutOption[] { GUILayout.ExpandWidth( true ), GUILayout.Height( height ) } ), lineTexture );
      GUILayout.Space( space );
    }

    public static bool EnumButtonList<EnumT>( Action<EnumT> onClick, Predicate<EnumT> filter = null, GUILayoutOption[] options = null )
    {
      foreach ( var eVal in Enum.GetValues( typeof( EnumT ) ) ) {
        bool filterPass = filter == null ||
                          filter( (EnumT)eVal );
        // Execute onClick if eVal passed the filter and the button is pressed.
        if ( filterPass && GUILayout.Button( MakeLabel( eVal.ToString().SplitCamelCase() ), EditorSkin.button, options ) ) {
          onClick( (EnumT)eVal );
          return true;
        }
      }
        
      return false;
    }

    public static Texture2D CreateColoredTexture( int width, int height, Color color )
    {
      Texture2D texture = new Texture2D( width, height );
      for ( int i = 0; i < width; ++i )
        for ( int j = 0; j < height; ++j )
          texture.SetPixel( i, j, color );

      texture.Apply();

      return texture;
    }

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
}
