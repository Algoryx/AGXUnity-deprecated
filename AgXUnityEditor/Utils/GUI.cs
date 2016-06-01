﻿using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;

namespace AgXUnityEditor.Utils
{
  public partial class GUI
  {
    /// <summary>
    /// Indent block.
    /// </summary>
    /// <example>
    /// using ( new GUI.Indent( 16.0f ) ) {
    ///   GUILayout.Label( "This label is indented 16 pixels." );
    /// }
    /// GUILayout.Label( "This label isn't indented." );
    /// </example>
    public class Indent : IDisposable
    {
      public Indent( float numPixels )
      {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space( numPixels );
        EditorGUILayout.BeginVertical();
      }

      public void Dispose()
      {
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
      }
    }

    public class Prefs
    {
      public static string CreateKey( object obj )
      {
        return obj.GetType().ToString();
      }

      public static bool GetOrCreateBool( object obj, bool defaultValue = false )
      {
        string key = CreateKey( obj );
        if ( EditorPrefs.HasKey( key ) )
          return EditorPrefs.GetBool( key );
        return SetBool( obj, defaultValue );
      }

      public static int GetOrCreateInt( object obj, int defaultValue = -1 )
      {
        string key = CreateKey( obj );
        if ( EditorPrefs.HasKey( key ) )
          return EditorPrefs.GetInt( key );
        return SetInt( obj, defaultValue );
      }

      public static bool SetBool( object obj, bool value )
      {
        string key = CreateKey( obj );
        EditorPrefs.SetBool( key, value );
        return value;
      }

      public static int SetInt( object obj, int value )
      {
        string key = CreateKey( obj );
        EditorPrefs.SetInt( key, value );
        return value;
      }

      public static void RemoveInt( object obj )
      {
        string key = CreateKey( obj );
        EditorPrefs.DeleteKey( key );
      }
    }

    public static void TargetEditorEnable<T>( T target, GUISkin skin ) where T : class
    {
      Tools.Tool.ActivateToolGivenTarget( target );
    }

    public static void TargetEditorDisable<T>( T target ) where T : class
    {
      var targetTool = Tools.Tool.GetActiveTool( target );
      if ( targetTool != null )
        Tools.Tool.RemoveActiveTool();
    }

    public static void PreTargetMembers<T>( T target, GUISkin skin ) where T : class
    {
      var targetTool = Tools.Tool.GetActiveTool( target );
      if ( targetTool != null )
        OnToolInspectorGUI( targetTool, target, skin );
    }

    public static string AddColorTag( string str, Color color )
    {
      return @"<color=" + color.ToHexStringRGBA() + @">" + str + @"</color>";
    }

    public static GUIContent MakeLabel( string text, bool bold = false, string toolTip = "" )
    {
      GUIContent label = new GUIContent();
      string boldBegin = bold ? "<b>" : "";
      string boldEnd   = bold ? "</b>" : "";
      label.text       = boldBegin + text + boldEnd;

      if ( toolTip != string.Empty )
        label.tooltip = toolTip;

      return label;
    }

    public static GUIContent MakeLabel( string text, int size, bool bold = false, string toolTip = "" )
    {
      GUIContent label = MakeLabel( text, bold, toolTip );
      label.text       = @"<size=" + size + @">" + label.text + @"</size>";
      return label;
    }

    public static GUIContent MakeLabel( string text, Color color, bool bold = false, string toolTip = "" )
    {
      GUIContent label = MakeLabel( text, bold, toolTip );
      label.text       = AddColorTag( text, color );
      return label;
    }

    public static GUIContent MakeLabel( string text, Color color, int size, bool bold = false, string toolTip = "" )
    {
      GUIContent label = MakeLabel( text, size, bold, toolTip );
      label.text       = AddColorTag( label.text, color );
      return label;
    }

    public static GUIStyle Align( GUIStyle style, TextAnchor anchor )
    {
      GUIStyle copy = new GUIStyle( style );
      copy.alignment = anchor;
      return copy;
    }

    public static Vector3 Vector3Field( GUIContent content, Vector3 value, GUIStyle style = null )
    {
      EditorGUILayout.BeginHorizontal();
      GUILayout.Label( content, style ?? Skin.label );
      value = EditorGUILayout.Vector3Field( "", value );
      EditorGUILayout.EndHorizontal();

      return value;
    }

    public static ValueT HandleDefaultAndUserValue<ValueT>( string name, DefaultAndUserValue<ValueT> valInField, GUISkin skin ) where ValueT : struct
    {
      ValueT newValue;
      MethodInfo floatMethod   = typeof( EditorGUILayout ).GetMethod( "FloatField", new[] { typeof( string ), typeof( float ), typeof( GUILayoutOption[] ) } );
      MethodInfo vector3Method = typeof( EditorGUILayout ).GetMethod( "Vector3Field", new[] { typeof( string ), typeof( Vector3 ), typeof( GUILayoutOption[] ) } );
      MethodInfo method        = typeof( ValueT ) == typeof( float ) ?
                                  floatMethod :
                                 typeof( ValueT ) == typeof( Vector3 ) ?
                                  vector3Method :
                                  null;
      if ( method == null )
        throw new NullReferenceException( "Unknown DefaultAndUserValue type: " + typeof( ValueT ).Name );

      {
        EditorGUILayout.BeginHorizontal();
        {
          var textDim = skin.label.CalcSize( new GUIContent( name.SplitCamelCase() ) );
          GUILayout.Label( name.SplitCamelCase(), skin.label, GUILayout.MaxWidth( textDim.x ) );
          if ( GUILayout.Button( MakeLabel( "Default" ), ConditionalCreateSelectedStyle( valInField.UseDefault, skin.button ) ) )
            valInField.UseDefault = true;
          if ( GUILayout.Button( MakeLabel( "User specified" ), ConditionalCreateSelectedStyle( !valInField.UseDefault, skin.button ) ) )
            valInField.UseDefault = false;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
          EditorGUI.BeginDisabledGroup( valInField.UseDefault );
          {
            newValue = (ValueT)method.Invoke( null, new object[] { "", valInField.Value, new GUILayoutOption[] { } } );
          }
          EditorGUI.EndDisabledGroup();
        }

        if ( GUILayout.Button( MakeLabel( "Update" ), skin.button ) )
          valInField.FireOnForcedUpdate();

        EditorGUILayout.EndHorizontal();
      }
      Separator();

      return newValue;
    }

    public static void HandleFrame( Frame frame, GUISkin skin, float numPixelsIndentation = 0.0f )
    {
      Tools.FrameTool frameTool = Tools.FrameTool.FindActive( frame );
      bool guiWasEnabled = UnityEngine.GUI.enabled;

      using ( new Indent( numPixelsIndentation ) ) {
        Separator();

        UnityEngine.GUI.enabled = true;
        GameObject newParent = (GameObject)EditorGUILayout.ObjectField( MakeLabel( "Parent" ), frame.Parent, typeof( GameObject ), true );
        UnityEngine.GUI.enabled = guiWasEnabled;

        if ( newParent != frame.Parent )
          frame.SetParent( newParent );

        frame.LocalPosition = Vector3Field( MakeLabel( "Local position" ), frame.LocalPosition, skin.label );
        frame.LocalRotation = Quaternion.Euler( Vector3Field( MakeLabel( "Local rotation" ), frame.LocalRotation.eulerAngles, skin.label ) );

        if ( frameTool != null ) {
          using ( new Indent( 12 ) ) {
            Separator();

            const char selectInSceneViewSymbol = 'p';//'\u2714';
            const char selectPointSymbol       = '\u22A1';
            const char selectEdgeSymbol        = '\u2196';
            const float toolButtonWidth        = 25.0f;
            const float toolButtonHeight       = 25.0f;
            GUIStyle toolButtonStyle           = new GUIStyle( skin.button );
            toolButtonStyle.fontSize           = 18;

            bool toggleSelectParent   = false;
            bool toggleFindGivenPoint = false;
            bool toggleSelectEdge     = false;

            EditorGUILayout.BeginHorizontal();
            {
              UnityEngine.GUI.enabled = true;
              GUILayout.Label( MakeLabel( "Tools:", true ), Align( skin.label, TextAnchor.MiddleLeft ), new GUILayoutOption[] { GUILayout.Width( 64 ), GUILayout.Height( 25 ) } );

              toggleSelectParent = GUILayout.Button( MakeLabel( selectInSceneViewSymbol.ToString(), false, "Select parent object in scene view" ),
                                                     ConditionalCreateSelectedStyle( frameTool.SelectParent, toolButtonStyle ),
                                                     new GUILayoutOption[] { GUILayout.Width( toolButtonWidth ), GUILayout.Height( toolButtonHeight ) } );
              UnityEngine.GUI.enabled = guiWasEnabled;

              toggleFindGivenPoint = GUILayout.Button( MakeLabel( selectPointSymbol.ToString(), false, "Find position and direction given surface" ),
                                                        ConditionalCreateSelectedStyle( frameTool.FindTransformGivenPointOnSurface, toolButtonStyle ),
                                                        new GUILayoutOption[] { GUILayout.Width( toolButtonWidth ), GUILayout.Height( toolButtonHeight ) } );
              toggleSelectEdge = GUILayout.Button( MakeLabel( selectEdgeSymbol.ToString(), false, "Find position and direction given edge" ),
                                                   ConditionalCreateSelectedStyle( frameTool.FindTransformGivenEdge, toolButtonStyle ),
                                                   new GUILayoutOption[] { GUILayout.Width( toolButtonWidth ), GUILayout.Height( toolButtonHeight ) } );
            }
            EditorGUILayout.EndHorizontal();

            if ( toggleSelectParent )
              frameTool.SelectParent = !frameTool.SelectParent;
            if ( toggleFindGivenPoint )
              frameTool.FindTransformGivenPointOnSurface = !frameTool.FindTransformGivenPointOnSurface;
            if ( toggleSelectEdge )
              frameTool.FindTransformGivenEdge = !frameTool.FindTransformGivenEdge;

            Separator();
          }
        }
      }
    }

    private static GUISkin m_editorGUISkin = null;
    public static GUISkin Skin
    {
      get
      {
        if ( m_editorGUISkin == null )
          m_editorGUISkin = Resources.Load<GUISkin>( "AgXEditorGUISkin" );
        return m_editorGUISkin ?? UnityEngine.GUI.skin;
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

    public static bool EnumButtonList<EnumT>( Action<EnumT> onClick, Predicate<EnumT> filter = null, GUIStyle style = null, GUILayoutOption[] options = null )
    {
      return EnumButtonList( onClick, filter, e => { return style ?? Skin.button; }, options );
    }

    public static bool EnumButtonList<EnumT>( Action<EnumT> onClick, Predicate<EnumT> filter = null, Func<EnumT, GUIStyle> styleCallback = null, GUILayoutOption[] options = null )
    {
      if ( styleCallback == null )
        styleCallback = e => { return Skin.button; };

      foreach ( var eVal in Enum.GetValues( typeof( EnumT ) ) ) {
        bool filterPass = filter == null ||
                          filter( (EnumT)eVal );
        // Execute onClick if eVal passed the filter and the button is pressed.
        if ( filterPass && GUILayout.Button( MakeLabel( eVal.ToString().SplitCamelCase() ), styleCallback( (EnumT)eVal ), options ) ) {
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

    public static GUIStyle CreateSelectedStyle( GUIStyle orgStyle )
    {
      GUIStyle selectedStyle = new GUIStyle( orgStyle );
      selectedStyle.normal = orgStyle.onActive;

      return selectedStyle;
    }

    public static Color ProBackgroundColor = new Color32( 56, 56, 56, 255 );
    public static Color IndieBackgroundColor = new Color32( 194, 194, 194, 255 );

    public static GUIStyle FadeNormalBackground( GUIStyle style, float t )
    {
      GUIStyle fadedStyle = new GUIStyle( style );
      Texture2D background = EditorGUIUtility.isProSkin ?
                               CreateColoredTexture( 1, 1, Color.Lerp( ProBackgroundColor, Color.white, t ) ) :
                               CreateColoredTexture( 1, 1, Color.Lerp( IndieBackgroundColor, Color.black, t ) );
      fadedStyle.normal.background = background;
      return fadedStyle;
    }

    public static GUIStyle ConditionalCreateSelectedStyle( bool selected, GUIStyle orgStyle )
    {
      return selected ? CreateSelectedStyle( orgStyle ) : orgStyle;
    }

    public static void OnToolInspectorGUI( Tools.Tool tool, object target, GUISkin skin )
    {
      if ( tool != null ) {
        tool.OnInspectorGUI( skin );
        //if ( target is UnityEngine.Object )
        //  EditorUtility.SetDirty( target as UnityEngine.Object );
      }
    }
  }
}
