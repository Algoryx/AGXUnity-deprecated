using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using GUI = AgXUnityEditor.Utils.GUI;

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

    [HideInInspector]
    public static readonly int ToggleButtonSize = 18;
    #endregion Static properties

    #region BuiltInToolsTool settings
    public Utils.KeyHandler BuiltInToolsTool_SelectGameObjectKeyHandler = new Utils.KeyHandler( KeyCode.S );
    public Utils.KeyHandler BuiltInToolsTool_SelectRigidBodyKeyHandler = new Utils.KeyHandler( KeyCode.B );
    public Utils.KeyHandler BuiltInToolsTool_PickHandlerKeyHandler = new Utils.KeyHandler( KeyCode.A );
    #endregion BuiltInToolsTool settings

    public bool AutoFetchAGXUnityDlls
    {
      get { return EditorData.Instance.GetData( this, "AutoFetchDlls" ).Bool; }
      set { EditorData.Instance.GetData( this, "AutoFetchDlls" ).Bool = value; }
    }

    public string AGXUnityCheckoutDir
    {
      get { return EditorData.Instance.GetData( this, "AGXUnityCheckoutDir" ).String; }
      set
      {
        var curr = EditorData.Instance.GetData( this, "AGXUnityCheckoutDir" ).String;
        if ( curr == value )
          return;

        EditorData.Instance.GetData( this, "AGXUnityCheckoutDir" ).String = value;
        LastDllCheck = EditorApplication.timeSinceStartup + 3.0;
      }
    }

    public bool AGXUnityCheckoutDirValid
    {
      get
      {
        return Directory.Exists( AGXUnityCheckoutDir ) &&
               File.Exists( AGXUnityCheckoutDir + @"\AgXUnity.dll" ) &&
               File.Exists( AGXUnityCheckoutDir + @"\AgXUnityEditor.dll" );
      }
    }

    [NonSerialized]
    public double LastDllCheck = -1.0;

    public bool ShouldCheckDllStatus( double checkFrequencySeconds )
    {
      if ( EditorApplication.isPlaying ||
           EditorApplication.isPaused ||
           EditorApplication.isCompiling ||
           !AutoFetchAGXUnityDlls ||
           !AGXUnityCheckoutDirValid )
        return false;

      if ( LastDllCheck > 0.0 && EditorApplication.timeSinceStartup - LastDllCheck < checkFrequencySeconds )
        return false;

      LastDllCheck = EditorApplication.timeSinceStartup;

      return true;
    }

    #region Rendering GUI
    public void OnInspectorGUI( GUISkin skin )
    {
      using ( GUI.AlignBlock.Center )
        GUILayout.Label( GUI.MakeLabel( "AgXUnity Editor Settings", 24, true ), skin.label );

      GUI.Separator3D();

      // BuiltInToolsTool settings GUI.
      {
        using ( GUI.AlignBlock.Center )
          GUILayout.Label( GUI.MakeLabel( "Built in tools", 16, true ), skin.label );

        //GUI.Separator();

        HandleKeyHandlerGUI( GUI.MakeLabel( "Select game object" ), BuiltInToolsTool_SelectGameObjectKeyHandler, skin );
        HandleKeyHandlerGUI( GUI.MakeLabel( "Select rigid body game object" ), BuiltInToolsTool_SelectRigidBodyKeyHandler, skin );
        HandleKeyHandlerGUI( GUI.MakeLabel( "Pick handler (scene view)" ), BuiltInToolsTool_PickHandlerKeyHandler, skin );

        GUI.Separator();
      }

      // Developer settings.
      {
        using ( GUI.AlignBlock.Center )
          GUILayout.Label( GUI.MakeLabel( "AGXUnity Developer", 16, true ), skin.label );

        AutoFetchAGXUnityDlls = GUI.Toggle( GUI.MakeLabel( "Auto fetch AGXUnity/AGXUnityEditor" ),
                                                           AutoFetchAGXUnityDlls,
                                                           skin.button,
                                                           GUI.Align( skin.label, TextAnchor.MiddleLeft ),
                                                           new GUILayoutOption[]
                                                           {
                                                             GUILayout.Width( ToggleButtonSize ),
                                                             GUILayout.Height( ToggleButtonSize )
                                                           },
                                                           new GUILayoutOption[]
                                                           {
                                                             GUILayout.Height( ToggleButtonSize )
                                                           } );

        using ( new EditorGUI.DisabledScope( !AutoFetchAGXUnityDlls ) ) {
          using ( new GUILayout.HorizontalScope() ) {
            GUILayout.Space( ToggleButtonSize + 4 );
            GUILayout.Label( GUI.MakeLabel( "Checkout directory" ), skin.label, GUILayout.Width( 160 ) );
            var statusColor = AGXUnityCheckoutDirValid ?
                                Color.Lerp( Color.white, Color.green, 0.2f ) :
                                Color.Lerp( Color.white, Color.red, 0.2f );
            var textFieldStyle = new GUIStyle( skin.textField );
            var prevColor = UnityEngine.GUI.backgroundColor;
            UnityEngine.GUI.backgroundColor = statusColor;
            AGXUnityCheckoutDir = GUILayout.TextField( AGXUnityCheckoutDir, skin.textField );
            UnityEngine.GUI.backgroundColor = prevColor;
            if ( GUILayout.Button( GUI.MakeLabel( "...", false, "Open file panel" ),
                                   skin.button,
                                   GUILayout.Width( 28 ) ) ) {
              AGXUnityCheckoutDir = EditorUtility.OpenFolderPanel( "AGXUnity checkout directory", AGXUnityCheckoutDir, "" );
            }
          }
        }

        GUI.Separator();
      }

      GUI.Separator3D();
    }

    private bool m_showDropDown = false;

    private void HandleKeyHandlerGUI( GUIContent name, Utils.KeyHandler keyHandler, GUISkin skin )
    {
      const int keyButtonWidth = 90;

      GUILayout.BeginHorizontal();
      {
        keyHandler.Enable = GUI.Toggle( name,
                                        keyHandler.Enable,
                                        skin.button,
                                        GUI.Align( skin.label, TextAnchor.MiddleLeft ),
                                        new GUILayoutOption[] { GUILayout.Width( ToggleButtonSize ), GUILayout.Height( ToggleButtonSize ) },
                                        new GUILayoutOption[] { GUILayout.Height( ToggleButtonSize ) } );
        GUILayout.FlexibleSpace();

        UnityEngine.GUI.enabled = keyHandler.Enable;

        for ( int iKey = 0; iKey < keyHandler.NumKeyCodes; ++iKey ) {
          GUIContent buttonLabel = keyHandler.IsDetectingKey( iKey ) ?
                                     GUI.MakeLabel( "Detecting..." ) :
                                     GUI.MakeLabel( keyHandler.Keys[ iKey ].ToString() );

          bool toggleDetecting = GUILayout.Button( buttonLabel, skin.button, GUILayout.Width( keyButtonWidth ), GUILayout.Height( ToggleButtonSize ) );
          if ( toggleDetecting )
            keyHandler.DetectKey( this, !keyHandler.IsDetectingKey( iKey ), iKey );
        }

        Rect dropDownButtonRect = new Rect();
        GUILayout.BeginVertical( GUILayout.Height( ToggleButtonSize ) );
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
