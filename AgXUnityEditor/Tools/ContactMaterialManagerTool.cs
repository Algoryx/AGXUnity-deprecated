using UnityEngine;
using UnityEditor;
using AgXUnity;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  [CustomTool( typeof ( ContactMaterialManager ) )]
  public class ContactMaterialManagerTool : Tool
  {
    public ContactMaterialManager Manager { get; private set; }

    public ContactMaterialManagerTool( ContactMaterialManager manager )
    {
      Manager = manager;
      Manager.RemoveNullEntries();
    }

    public override void OnPreTargetMembersGUI( GUISkin skin )
    {
      OnContactMaterialsList( skin );
    }

    private EditorDataEntry FoldoutDataEntry { get { return EditorData.Instance.GetData( Manager, "ContactMaterials" ); } }

    private void OnContactMaterialsList( GUISkin skin )
    {
      ContactMaterial contactMaterialToRemove = null;

      GUILayout.BeginVertical();
      {
        if ( GUI.Foldout( FoldoutDataEntry, GUI.MakeLabel( "Contact Materials [" + Manager.ContactMaterialEntries.Length + "]" ), skin ) ) {
          var contactMaterials = Manager.ContactMaterials;
          using ( new GUI.Indent( 12 ) ) {
            foreach ( var contactMaterial in contactMaterials ) {
              GUI.Separator();

              bool foldoutActive = false;

              GUILayout.BeginHorizontal();
              {
                foldoutActive = GUI.Foldout( EditorData.Instance.GetData( Manager, contactMaterial.name ), GUI.MakeLabel( contactMaterial.name ), skin );
                using ( WireTool.NodeListButtonColor )
                  if ( GUILayout.Button( GUI.MakeLabel( GUI.Symbols.ListEraseElement.ToString(), false, "Erase this element" ),
                                         skin.button,
                                         new GUILayoutOption[] { GUILayout.Width( 20 ), GUILayout.Height( 14 ) } ) )
                    contactMaterialToRemove = contactMaterial;
              }
              GUILayout.EndHorizontal();

              if ( foldoutActive ) {
                using ( new GUI.Indent( 12 ) )
                  BaseEditor<ContactMaterial>.Update( contactMaterial, contactMaterial, skin );
              }
            }

            if ( contactMaterials.Length == 0 )
              GUILayout.Label( GUI.MakeLabel( "Empty", true ), skin.textArea );
            else
              GUI.Separator();
          }
        }
      }
      GUILayout.EndVertical();

      GUI.HandleDragDrop<ContactMaterial>( GUILayoutUtility.GetLastRect(),
                                           Event.current,
                                           ( contactMaterial ) =>
                                           {
                                             Manager.Add( contactMaterial );

                                             FoldoutDataEntry.Bool = true;
                                           } );

      if ( contactMaterialToRemove != null )
        Manager.Remove( contactMaterialToRemove );
    }
  }
}
