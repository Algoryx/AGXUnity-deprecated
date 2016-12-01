using System;
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
    public static class Symbols
    {
      public const char ToggleEnabled           = '\u2714';
      public const char ToggleDisabled          = ' ';

      public const char ShapeResizeTool         = '\u21C4';
      public const char ShapeCreateTool         = '\u210C';

      public const char SelectInSceneViewTool   = 'p';
      public const char SelectPointTool         = '\u22A1';
      public const char SelectEdgeTool          = '\u2196';
      public const char PositionHandleTool      = 'L';

      public const char ConstraintCreateTool    = '\u2102';

      public const char DisableCollisionsTool   = '\u2229';

      public const char ListInsertElementBefore = '\u21B0';
      public const char ListInsertElementAfter  = '\u21B2';
      public const char ListEraseElement        = 'x';

      public const char Synchronized            = '\u2194';
    }

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
        GUILayout.BeginHorizontal();
        GUILayout.Space( numPixels );
        GUILayout.BeginVertical();
      }

      public void Dispose()
      {
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
      }
    }

    public class AlignBlock : IDisposable
    {
      public enum Alignment { Left, Center, Right };

      public static AlignBlock Left { get { return new AlignBlock( Alignment.Left ); } }
      public static AlignBlock Center { get { return new AlignBlock( Alignment.Center ); } }
      public static AlignBlock Right { get { return new AlignBlock( Alignment.Right ); } }

      private Alignment m_alignment = Alignment.Center;

      private AlignBlock( Alignment alignment )
      {
        m_alignment = alignment;

        GUILayout.BeginHorizontal();
        if ( m_alignment != Alignment.Left )
          GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();
      }

      public void Dispose()
      {
        GUILayout.EndVertical();
        if ( m_alignment == Alignment.Center )
          GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
      }
    }

    public class ColorBlock : IDisposable
    {
      private Color m_prevColor = default( Color );

      public ColorBlock( Color color )
      {
        m_prevColor = UnityEngine.GUI.color;
        UnityEngine.GUI.color = color;
      }

      public void Dispose()
      {
        UnityEngine.GUI.color = m_prevColor;
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
      KeyHandler.HandleDetectKeyOnEnable( target );

      Tools.Tool.ActivateToolGivenTarget( target );
    }

    public static bool TargetEditorOnInspectorGUI<T>( T target, GUISkin skin ) where T : class
    {
      return KeyHandler.HandleDetectKeyOnGUI( target, Event.current );
    }

    public static void TargetEditorDisable<T>( T target ) where T : class
    {
      KeyHandler.HandleDetectKeyOnDisable( target );

      var targetTool = Tools.Tool.GetActiveTool( target );
      if ( targetTool != null )
        Tools.Tool.RemoveActiveTool();
    }

    private enum TargetToolGUICallbackType
    {
      Pre,
      Post
    }

    public static void PreTargetMembers<T>( T target, GUISkin skin ) where T : class
    {
      OnToolInspectorGUI( target, skin, TargetToolGUICallbackType.Pre );
    }

    public static void PostTargetMembers<T>( T target, GUISkin skin ) where T : class
    {
      OnToolInspectorGUI( target, skin, TargetToolGUICallbackType.Post );
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
      GUILayout.BeginHorizontal();
      GUILayout.Label( content, style ?? Skin.label );
      value = EditorGUILayout.Vector3Field( "", value );
      GUILayout.EndHorizontal();

      return value;
    }

    public static GUILayoutOption[] DefaultToggleButtonOptions { get { return new GUILayoutOption[] { GUILayout.Width( 20 ), GUILayout.Height( 14 ) }; } }
    public static GUILayoutOption[] DefaultToggleLabelOptions { get { return new GUILayoutOption[] { }; } }

    public static bool Toggle( GUIContent content, bool value, GUIStyle buttonStyle, GUIStyle labelStyle, GUILayoutOption[] buttonOptions = null, GUILayoutOption[] labelOptions = null )
    {
      if ( buttonOptions == null )
        buttonOptions = DefaultToggleButtonOptions;
      if ( labelOptions == null )
        labelOptions = DefaultToggleLabelOptions;

      bool buttonDown = false;
      GUILayout.BeginHorizontal();
      {
        string buttonText = value ? Symbols.ToggleEnabled.ToString() : Symbols.ToggleDisabled.ToString();
        buttonDown = GUILayout.Button( MakeLabel( buttonText, false, content.tooltip ), ConditionalCreateSelectedStyle( value, buttonStyle ), buttonOptions );
        GUILayout.Label( content, labelStyle, labelOptions );
      }
      GUILayout.EndHorizontal();
      
      return buttonDown ? !value : value;
    }

    public static ValueT HandleDefaultAndUserValue<ValueT>( string name, DefaultAndUserValue<ValueT> valInField, GUISkin skin ) where ValueT : struct
    {
      bool guiWasEnabled       = UnityEngine.GUI.enabled;
      ValueT newValue          = default( ValueT );
      MethodInfo floatMethod   = typeof( EditorGUILayout ).GetMethod( "FloatField", new[] { typeof( string ), typeof( float ), typeof( GUILayoutOption[] ) } );
      MethodInfo vector3Method = typeof( EditorGUILayout ).GetMethod( "Vector3Field", new[] { typeof( string ), typeof( Vector3 ), typeof( GUILayoutOption[] ) } );
      MethodInfo method        = typeof( ValueT ) == typeof( float ) ?
                                  floatMethod :
                                 typeof( ValueT ) == typeof( Vector3 ) ?
                                  vector3Method :
                                  null;
      if ( method == null )
        throw new NullReferenceException( "Unknown DefaultAndUserValue type: " + typeof( ValueT ).Name );

      bool useDefaultToggled = false;
      bool updateDefaultValue = false;
      GUILayout.BeginHorizontal();
      {
        // Note that we're checking if the value has changed!
        useDefaultToggled = Toggle( MakeLabel( name.SplitCamelCase(), false, "If checked - value will be default. Uncheck to manually enter value." ),
                                    valInField.UseDefault,
                                    skin.button,
                                    Align( skin.label, TextAnchor.MiddleLeft ),
                                    new GUILayoutOption[] { GUILayout.Width( 22 ) },
                                    new GUILayoutOption[] { GUILayout.MaxWidth( 120 ) } ) != valInField.UseDefault;
        UnityEngine.GUI.enabled = !valInField.UseDefault;
        GUILayout.FlexibleSpace();
        newValue = (ValueT)method.Invoke( null, new object[] { "", valInField.Value, new GUILayoutOption[] { } } );
        UnityEngine.GUI.enabled = valInField.UseDefault;
        updateDefaultValue = GUILayout.Button( MakeLabel( "Update", false, "Update default value" ), skin.button, GUILayout.Width( 52 ) );
        UnityEngine.GUI.enabled = guiWasEnabled;
      }
      GUILayout.EndHorizontal();

      if ( useDefaultToggled ) {
        valInField.UseDefault = !valInField.UseDefault;
        updateDefaultValue    = valInField.UseDefault;

        // We don't want the default value to be written to
        // the user specified.
        if ( !valInField.UseDefault )
          newValue = valInField.UserValue;
      }

      if ( updateDefaultValue )
        valInField.FireOnForcedUpdate();

      return newValue;
    }

    public class ToolButtonData
    {
      public static GUILayoutOption Width { get { return GUILayout.Width( 25f ); } }
      public static GUILayoutOption Height { get { return GUILayout.Height( 25f ); } }
      public static GUIStyle Style( GUISkin skin, int fontSize = 18 )
      {
        GUIStyle style = new GUIStyle( skin.button );
        style.fontSize = fontSize;
        return style;
      }
      public static ColorBlock ColorBlock { get { return new ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.yellow, 0.1f ) ); } }
    }

    public static void ToolsLabel( GUISkin skin )
    {
      GUILayout.Label( GUI.MakeLabel( "Tools:", true ), Align( skin.label, TextAnchor.MiddleLeft ), new GUILayoutOption[] { GUILayout.Width( 64 ), GUILayout.Height( 25 ) } );
    }

    public static bool ToolButton( char symbol, bool active, string toolTip, GUISkin skin, int fontSize = 18 )
    {
      return GUILayout.Button( MakeLabel( symbol.ToString(), false, toolTip ),
                               ConditionalCreateSelectedStyle( active, ToolButtonData.Style( skin, fontSize ) ),
                               ToolButtonData.Width,
                               ToolButtonData.Height );
    }

    public static void HandleFrame( Frame frame, GUISkin skin, float numPixelsIndentation = 0.0f, bool includeFrameToolIfPresent = true )
    {
      bool guiWasEnabled = UnityEngine.GUI.enabled;

      Undo.RecordObject( frame, "HandleFrame" );
      using ( new Indent( numPixelsIndentation ) ) {
        UnityEngine.GUI.enabled = true;
        GameObject newParent = (GameObject)EditorGUILayout.ObjectField( MakeLabel( "Parent" ), frame.Parent, typeof( GameObject ), true );
        UnityEngine.GUI.enabled = guiWasEnabled;

        if ( newParent != frame.Parent )
          frame.SetParent( newParent );

        frame.LocalPosition = Vector3Field( MakeLabel( "Local position" ), frame.LocalPosition, skin.label );

        // Converting from quaternions to Euler - make sure the actual Euler values has
        // changed before updating local rotation to not mess up the undo stack.
        Vector3 inputEuler  = frame.LocalRotation.eulerAngles;
        Vector3 outputEuler = Vector3Field( MakeLabel( "Local rotation" ), inputEuler, skin.label );
        if ( !ValueType.Equals( inputEuler, outputEuler ) )
          frame.LocalRotation = Quaternion.Euler( outputEuler );

        Separator();

        Tools.FrameTool frameTool = null;
        if ( includeFrameToolIfPresent && ( frameTool = Tools.FrameTool.FindActive( frame ) ) != null )
          using ( new Indent( 12 ) )
            frameTool.OnPreTargetMembersGUI( skin );
      }
    }

    public static bool Foldout( EditorDataEntry state, GUIContent label, GUISkin skin )
    {
      GUILayout.BeginHorizontal();
      {
        state.Bool = GUILayout.Button( MakeLabel( state.Bool ? "-" : "+" ), skin.button, new GUILayoutOption[] { GUILayout.Width( 20 ), GUILayout.Height( 14 ) } ) ? !state.Bool : state.Bool;
        GUILayout.Label( label, skin.label, GUILayout.ExpandWidth( true ) );
        if ( GUILayoutUtility.GetLastRect().Contains( Event.current.mousePosition ) && Event.current.type == EventType.MouseDown && Event.current.button == 0 ) {
          state.Bool = !state.Bool;
          GUIUtility.ExitGUI();
        }
      }
      GUILayout.EndHorizontal();

      return state.Bool;
    }

    /// <summary>
    /// Handles drag and drop over given area.
    /// </summary>
    /// <example>
    /// GUILayout.Label( "Drag and drop asset here to apply the stuff" );
    /// Utils.GUI.HandleDragDrop&lt; MyAsset &gt;( GUILayoutUtility.GetLastRect(),
    ///                                            Event.current,
    ///                                            ( myAsset ) => { ApplyStuff( myAsset ); } );
    /// </example>
    /// <typeparam name="T">Expected dropped object type.</typeparam>
    /// <param name="dropArea">Drop rect.</param>
    /// <param name="current">Current event.</param>
    /// <param name="onDrop">Callback when an object has been dropped.</param>
    public static void HandleDragDrop<T>( Rect dropArea, Event current, Action<T> onDrop ) where T : UnityEngine.Object
    {
      bool isDragDropEventInDropArea = ( current.type == EventType.DragPerform || current.type == EventType.DragUpdated ) && dropArea.Contains( current.mousePosition );
      if ( !isDragDropEventInDropArea )
        return;

      bool validObject = DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[ 0 ] is T;
      DragAndDrop.visualMode = validObject ?
                                  DragAndDropVisualMode.Copy :
                                  DragAndDropVisualMode.Rejected;

      if ( Event.current.type == EventType.DragPerform && validObject ) {
        DragAndDrop.AcceptDrag();

        onDrop( DragAndDrop.objectReferences[ 0 ] as T );
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
      Texture2D lineTexture = EditorGUIUtility.isProSkin ?
                                Texture2D.whiteTexture :
                                Texture2D.blackTexture;

      GUILayout.Space( space );
      EditorGUI.DrawPreviewTexture( EditorGUILayout.GetControlRect( new GUILayoutOption[] { GUILayout.ExpandWidth( true ), GUILayout.Height( height ) } ), lineTexture );
      GUILayout.Space( space );
    }

    public static void Separator3D( float space = 2.0f )
    {
      GUILayout.Space( space );
      EditorGUI.DrawPreviewTexture( EditorGUILayout.GetControlRect( new GUILayoutOption[] { GUILayout.ExpandWidth( true ), GUILayout.Height( 1f ) } ), Texture2D.whiteTexture );
      EditorGUI.DrawPreviewTexture( EditorGUILayout.GetControlRect( new GUILayoutOption[] { GUILayout.ExpandWidth( true ), GUILayout.Height( 1f ) } ), Texture2D.blackTexture );
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

    private static void OnToolInspectorGUI( object target, GUISkin skin, TargetToolGUICallbackType callbackType )
    {
      var targetTool = Tools.Tool.GetActiveTool( target );
      if ( targetTool == null )
        return;

      if ( callbackType == TargetToolGUICallbackType.Pre )
        targetTool.OnPreTargetMembersGUI( skin );
      else if ( callbackType == TargetToolGUICallbackType.Post )
        targetTool.OnPostTargetMembersGUI( skin );
    }
  }
}
