using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;

namespace AgXUnityEditor.Utils
{
  public partial class GUI
  {
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

    public static void PreTargetMembers<T>( T target, GUISkin skin ) where T : class
    {
      if ( target is Wire )
        PreTargetMembers( target as Wire, skin );
      else if ( target is Constraint )
        PreTargetMembers( target as Constraint, skin );
    }

    public static void TargetEditorDisable<T>( T target ) where T : class
    {
      if ( target is Wire )
        TargetEditorDisable( target as Wire );
      else if ( target is Constraint )
        TargetEditorDisable( target as Constraint );
    }

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

    public static void HandleFrame( Frame frame, GUISkin skin, bool includeParentObjectField = true, float numPixelsIndentation = 0.0f )
    {
      EditorGUILayout.BeginHorizontal();
      GUILayout.Space( numPixelsIndentation );
      if ( includeParentObjectField ) {
        GameObject newParent = (GameObject)EditorGUILayout.ObjectField( MakeLabel( "Parent" ), frame.Parent, typeof( GameObject ), true );
        if ( newParent != frame.Parent )
          frame.SetParent( newParent );
      }
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.BeginHorizontal();
      GUILayout.Space( numPixelsIndentation );
      Tools.FrameTool frameTool = Tools.FrameTool.FindActive( frame );
      if ( GUILayout.Button( MakeLabel( "Edit" ), Utils.GUI.ConditionalCreateSelectedStyle( frameTool != null, skin.button ), new GUILayoutOption[] { GUILayout.Width( 32 ), GUILayout.Height( 16 * 2 ) } ) ) {
        if ( frameTool != null ) {
          frameTool.Remove();
          frameTool = null;
        }
        else
          frameTool = Manager.ActivateTool<Tools.FrameTool>( new Tools.FrameTool( frame ) );
      }
      EditorGUILayout.BeginVertical();
      frame.LocalPosition = Vector3Field( MakeLabel( "Local position" ), frame.LocalPosition, skin.label );
      frame.LocalRotation = Quaternion.Euler( Vector3Field( MakeLabel( "Local rotation" ), frame.LocalRotation.eulerAngles, skin.label ) );
      EditorGUILayout.EndVertical();
      EditorGUILayout.EndHorizontal();
    }

    public static GUIStyle EditorSkinLabel( TextAnchor alignment = TextAnchor.MiddleCenter )
    {
      GUIStyle style = new GUIStyle( Skin.label );
      style.alignment = alignment;
      return style;
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

    public static GUIStyle ConditionalCreateSelectedStyle( bool selected, GUIStyle orgStyle )
    {
      return selected ? CreateSelectedStyle( orgStyle ) : orgStyle;
    }

    public static void OnToolInspectorGUI( Tools.Tool tool, object target, GUISkin skin )
    {
      if ( tool != null ) {
        Separator( 4.0f );
        tool.OnInspectorGUI( skin );
        if ( target is UnityEngine.Object )
          EditorUtility.SetDirty( target as UnityEngine.Object );
        Separator( 4.0f );
      }
      else
        Separator();
    }
  }
}
