using System;
using UnityEngine;
using UnityEditor;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor
{
  public static class EditorSettings
  {

  }
}

namespace AgXUnityEditor.Windows
{
  public class EditorSettings : EditorWindow
  {
    [MenuItem( "AgXUnity/Settings..." )]
    private static void Init()
    {
      Instance = GetWindowWithRect<EditorSettings>( new Rect( new Vector2( 300, 300 ), new Vector2( 520, 440 ) ), true, "AgXUnity Global Settings", true );
    }

    private GUISkin m_skin = null;
    public GUISkin Skin
    {
      get
      {
        if ( m_skin == null ) {
          m_skin = EditorGUIUtility.GetBuiltinSkin( EditorSkin.Inspector );
          m_skin.label.richText = true;
          m_skin.button.richText = true;
          m_skin.toggle.richText = true;
        }

        return m_skin;
      }
    }

    public static EditorSettings Instance { get; private set; }

    private void OnGUI()
    {
      using ( new GUI.AlignBlock( GUI.AlignBlock.Alignment.Center ) )
        GUILayout.Label( GUI.MakeLabel( "AgXUnity Editor Settings", 24, true ), Skin.label );
      GUI.Separator3D();
    }
  }
}
