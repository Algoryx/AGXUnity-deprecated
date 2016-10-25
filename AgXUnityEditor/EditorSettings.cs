using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using GUI = AgXUnityEditor.Utils.GUI;
using Assembly = System.Reflection.Assembly;

namespace AgXUnityEditor
{
  public class EditorSettings : ScriptableObject
  {
    #region Static properties
    [HideInInspector]
    public static string AgXUnityPath { get { return @"Assets/AgXUnity"; } }

    [HideInInspector]
    public static string AgXUnityEditorPath { get { return AgXUnityPath + @"/Editor"; } }

    [HideInInspector]
    public static string AgXUnityEditorDataPath { get { return AgXUnityEditorPath + @"/Data"; } }

    [HideInInspector]
    public static bool AgXUnityFolderExist { get { return AssetDatabase.IsValidFolder( AgXUnityPath ); } }

    [HideInInspector]
    public static bool AgXUnityEditorFolderExist { get { return AssetDatabase.IsValidFolder( AgXUnityEditorPath ); } }

    [HideInInspector]
    public static bool AgXUnityEditorDataFolderExist { get { return AssetDatabase.IsValidFolder( AgXUnityEditorDataPath ); } }

    [HideInInspector]
    public static EditorSettings Instance { get { return GetOrCreateInstance(); } }
    #endregion Static properties

    #region BuiltInToolsTool settings
    public Utils.KeyHandler BuiltInToolsTool_SelectGameObjectKeyHandler = new Utils.KeyHandler( KeyCode.S );
    public Utils.KeyHandler BuiltInToolsTool_PickHandlerKeyHandler = new Utils.KeyHandler( KeyCode.A );
    #endregion BuiltInToolsTool settings

    #region Rendering GUI
    public void OnInspectorGUI( GUISkin skin )
    {
      using ( new GUI.AlignBlock( GUI.AlignBlock.Alignment.Center ) )
        GUILayout.Label( GUI.MakeLabel( "AgXUnity Editor Settings", 24, true ), skin.label );

      GUI.Separator3D();

      // Debug render manager.
      {
        using ( new GUI.AlignBlock( GUI.AlignBlock.Alignment.Center ) )
          GUILayout.Label( GUI.MakeLabel( "Debug render manager", 16, true ), skin.label );
      }

      // BuiltInToolsTool settings GUI.
      {
        using ( new GUI.AlignBlock( GUI.AlignBlock.Alignment.Center ) )
          GUILayout.Label( GUI.MakeLabel( "Built in tools", 16, true ), skin.label );

        GUI.Separator();

        HandleKeyHandlerGUI( GUI.MakeLabel( "Select game object" ), BuiltInToolsTool_SelectGameObjectKeyHandler, skin );
        HandleKeyHandlerGUI( GUI.MakeLabel( "Pick handler (scene view)" ), BuiltInToolsTool_PickHandlerKeyHandler, skin );
      }

      GUI.Separator3D();
    }

    private bool m_showDropDown = false;

    private void HandleKeyHandlerGUI( GUIContent name, Utils.KeyHandler keyHandler, GUISkin skin )
    {
      const int keyButtonWidth = 90;
      const int keyButtonHeight = 18;

      GUILayout.BeginHorizontal();
      {
        keyHandler.Enable = GUI.Toggle( name,
                                        keyHandler.Enable,
                                        skin.button,
                                        GUI.Align( skin.label, TextAnchor.MiddleLeft ),
                                        new GUILayoutOption[] { GUILayout.Width( keyButtonHeight ), GUILayout.Height( keyButtonHeight ) },
                                        new GUILayoutOption[] { GUILayout.Height( keyButtonHeight ) } );
        GUILayout.FlexibleSpace();

        UnityEngine.GUI.enabled = keyHandler.Enable;

        for ( int iKey = 0; iKey < keyHandler.NumKeyCodes; ++iKey ) {
          GUIContent buttonLabel = keyHandler.IsDetectingKey( iKey ) ?
                                     GUI.MakeLabel( "Detecting..." ) :
                                     GUI.MakeLabel( keyHandler.Keys[ iKey ].ToString() );

          bool toggleDetecting = GUILayout.Button( buttonLabel, skin.button, GUILayout.Width( keyButtonWidth ), GUILayout.Height( keyButtonHeight ) );
          if ( toggleDetecting )
            keyHandler.DetectKey( this, !keyHandler.IsDetectingKey( iKey ), iKey );
        }

        Rect dropDownButtonRect = new Rect();
        GUILayout.BeginVertical( GUILayout.Height( keyButtonHeight ) );
        {
          GUIStyle tmp = new GUIStyle( skin.button );
          tmp.fontSize = 6;

          m_showDropDown = GUILayout.Button( GUI.MakeLabel( "v", true ), tmp, GUILayout.Width( 16 ), GUILayout.Height( 14 ) ) ?
                             !m_showDropDown :
                              m_showDropDown;
          dropDownButtonRect = GUILayoutUtility.GetLastRect();
          GUILayout.FlexibleSpace();
        }
        GUILayout.EndVertical();

        UnityEngine.GUI.enabled = true;

        if ( m_showDropDown && dropDownButtonRect.Contains( Event.current.mousePosition ) ) {
          GenericMenu menu = new GenericMenu();
          menu.AddItem( GUI.MakeLabel( "Reset to default" ), false, () =>
          {
            if ( EditorUtility.DisplayDialog( "Reset to default", "Reset key(s) to default?", "OK", "Cancel" ) )
              keyHandler.ResetToDefault();
          } );
          menu.AddItem( GUI.MakeLabel( "Add key" ), false, () =>
          {
            keyHandler.Add( KeyCode.None );
          } );

          if ( keyHandler.NumKeyCodes > 1 ) {
            menu.AddItem( GUI.MakeLabel( "Remove key" ), false, () =>
            {
              if ( EditorUtility.DisplayDialog( "Remove key", "Remove key: " + keyHandler[ keyHandler.NumKeyCodes - 1 ].ToString() + "?", "OK", "Cancel" ) )
                keyHandler.Remove( keyHandler.NumKeyCodes - 1 );
            } );
          }

          menu.ShowAsContext();
        }
      }
      GUILayout.EndHorizontal();

      if ( UnityEngine.GUI.changed )
        EditorUtility.SetDirty( this );
    }
    #endregion Rendering GUI

    #region Static singleton initialization methods
    public static bool PrepareEditorDataFolder()
    {
      if ( !AgXUnityFolderExist ) {
        Debug.LogError( "AgXUnity folder is not present in the Assets folder. Something is wrong with the configuration." );
        return false;
      }

      if ( !AgXUnityEditorFolderExist ) {
        Debug.LogError( "AgXUnity/Editor folder is not present in the Assets folder. Something is wrong with the configuration." );
        return false;
      }

      if ( !AgXUnityEditorDataFolderExist ) {
        AssetDatabase.CreateFolder( AgXUnityEditorPath, "Data" );
        AssetDatabase.SaveAssets();
      }

      return true;
    }

    public static T GetOrCreateEditorDataFolderFileInstance<T>( string name ) where T : ScriptableObject
    {
      if ( !PrepareEditorDataFolder() )
        return null;

      string settingsPathAndName = AgXUnityEditorDataPath + @name;
      T instance = AssetDatabase.LoadAssetAtPath<T>( settingsPathAndName );
      if ( instance == null ) {
        instance = CreateInstance<T>();
        AssetDatabase.CreateAsset( instance, settingsPathAndName );
        AssetDatabase.SaveAssets();
      }

      return instance;
    }

    [ MenuItem( "AgXUnity/Settings..." ) ]
    private static void Init()
    {
      EditorSettings instance = GetOrCreateInstance();
      if ( instance == null )
        return;

      EditorUtility.FocusProjectWindow();
      Selection.activeObject = instance;
    }

    private static EditorSettings GetOrCreateInstance()
    {
      if ( m_instance != null )
        return m_instance;

      return ( m_instance = GetOrCreateEditorDataFolderFileInstance<EditorSettings>( "/Settings.asset" ) );
    }

    [NonSerialized]
    private static EditorSettings m_instance = null;
    #endregion Static singleton initialization methods
  }

  [CustomEditor( typeof( EditorSettings ) )]
  public class EditorSettingsEditor : BaseEditor<EditorSettings>
  {
    protected override bool OverrideOnInspectorGUI( EditorSettings target, GUISkin skin )
    {
      EditorSettings.Instance.OnInspectorGUI( CurrentSkin );
      return true;
    }
  }
}
