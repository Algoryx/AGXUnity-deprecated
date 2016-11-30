using System;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using AgXUnity;
using AgXUnity.Utils;
using UnityEngine;
using UnityEditor;

namespace AgXUnityEditor
{
  public class BaseEditor<T> : Editor where T : class
  {
    public static GUISkin CurrentSkin { get { return EditorGUIUtility.GetBuiltinSkin( EditorSkin.Inspector ); } }

    public static bool Update( object obj, T target, GUISkin skin = null )
    {
      if ( obj == null )
        return false;

      var objAsUnityObject = obj as UnityEngine.Object;
      if ( objAsUnityObject != null )
        Undo.RecordObject( objAsUnityObject, "Changes to " + objAsUnityObject.name );

      bool updates = DrawMembersGUI( obj, target, skin ?? CurrentSkin );

      // Assets aren't saved with the project if they aren't flagged
      // as dirty. Also true for AssetDatabase.SaveAssets.
      if ( updates ) {
        if ( objAsUnityObject != null )
          EditorUtility.SetDirty( objAsUnityObject );
        if ( obj != target && typeof( UnityEngine.Object ).IsAssignableFrom( typeof( T ) ) )
          EditorUtility.SetDirty( target as UnityEngine.Object );
      }

      return updates;
    }

    public override sealed void OnInspectorGUI()
    {
      if ( Utils.GUI.TargetEditorOnInspectorGUI<T>( target as T, CurrentSkin ) )
        return;

      if ( OverrideOnInspectorGUI( target as T, CurrentSkin ) )
        return;

      Update( target, target as T, CurrentSkin );
    }

    public void OnEnable()
    {
      GUISkin guiSkin = EditorGUIUtility.GetBuiltinSkin( EditorSkin.Inspector );
      guiSkin.label.richText = true;
      guiSkin.toggle.richText = true;
      guiSkin.button.richText = true;
      guiSkin.textArea.richText = true;
      guiSkin.textField.richText = true;

      Utils.GUI.TargetEditorEnable<T>( target as T, guiSkin );

      // It's possible to detect when this editor/object becomes selected.
      //if ( Application.isEditor && target != null )
      //  Debug.Log( "Create!" );
    }

    public void OnDestroy()
    {
      Utils.GUI.TargetEditorDisable<T>( target as T );

      // It's possible to detect when this editor/object is being deleted
      // e.g., when the user presses delete.
      if ( !Application.isPlaying && Application.isEditor && target == null )
        AgXUnity.Rendering.DebugRenderManager.OnEditorDestroy();
    }

    protected virtual bool OverrideOnInspectorGUI( T target, GUISkin skin ) { return false; }

    private static bool ShouldBeShownInInspector( MemberInfo memberInfo )
    {
      if ( memberInfo == null )
        return false;

      // Override hidden in inspector.
      if ( memberInfo.IsDefined( typeof( HideInInspector ), true ) )
        return false;

      // In general, don't show UnityEngine objects unless ShowInInspector is set.
      bool show = memberInfo.IsDefined( typeof( ShowInInspector ), true ) ||
                  !(memberInfo.DeclaringType.Namespace != null && memberInfo.DeclaringType.Namespace.Contains( "UnityEngine" ));

      return show;
    }

    private static bool ShouldBeShownInInspector( MethodInfo methodInfo )
    {
      if ( methodInfo == null )
        return false;

      if ( methodInfo.IsDefined( typeof( InvokableInInspector ), false ) )
        return true;

      return false;
    }

    private static bool DrawMembersGUI( object obj, T target, GUISkin skin )
    {
      if ( obj == null )
        return false;

      if ( obj.GetType() == typeof( CollisionGroupEntryPair ) )
        return HandleCollisionGroupEntryPair( obj as CollisionGroupEntryPair, skin );

      if ( obj == target )
        Utils.GUI.PreTargetMembers( target, skin );

      bool changed = false;
      InvokeWrapper[] fieldsAndProperties = InvokeWrapper.FindFieldsAndProperties( obj );
      foreach ( InvokeWrapper wrapper in fieldsAndProperties )
        if ( ShouldBeShownInInspector( wrapper.Member ) )
          changed = HandleType( wrapper, target, skin ) || changed;

      var methods = from methodInfo in obj.GetType().GetMethods( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static )
                    where
                      ShouldBeShownInInspector( methodInfo )
                    select methodInfo;
      methods.ToList().ForEach( methodInfo => changed = HandleMethod( methodInfo, target, skin ) || changed );

      if ( obj == target )
        Utils.GUI.PostTargetMembers( target, skin );

      return changed;
    }

    private static bool HandleMethod( MethodInfo methodInfo, T target, GUISkin skin )
    {
      if ( methodInfo == null )
        return false;

      object[] attributes = methodInfo.GetCustomAttributes( typeof( InvokableInInspector ), false );
      GUIContent label = null;
      if ( attributes.Length > 0 && ( attributes[ 0 ] as InvokableInInspector ).Label != "" )
        label = Utils.GUI.MakeLabel( ( attributes[ 0 ] as InvokableInInspector ).Label );
      else
        label = MakeLabel( methodInfo );

      bool invoked = false;
      if ( GUILayout.Button( label, skin.button, new GUILayoutOption[]{} ) ) {
        methodInfo.Invoke( target, new object[] { } );
        invoked = true;
      }

      return invoked;
    }

    public static bool HandleType( InvokeWrapper wrapper, T target, GUISkin skin )
    {
      object value = null;
      bool isNullable = false;
      Type type = wrapper.GetContainingType();
      if ( type == typeof( Vector4 ) && wrapper.CanRead() ) {
        Vector4 valInField = wrapper.Get<Vector4>();
        value = EditorGUILayout.Vector4Field( MakeLabel( wrapper.Member ).text, valInField );
      }
      else if ( type == typeof( Vector3 ) && wrapper.CanRead() ) {
        Vector3 valInField = wrapper.Get<Vector3>();
        GUILayout.BeginHorizontal();
        {
          GUILayout.Label( MakeLabel( wrapper.Member ) );
          value = EditorGUILayout.Vector3Field( "", valInField );
        }
        GUILayout.EndHorizontal();
      }
      else if ( type == typeof( Vector2 ) && wrapper.CanRead() ) {
        Vector2 valInField = wrapper.Get<Vector2>();
        value = EditorGUILayout.Vector2Field( MakeLabel( wrapper.Member ).text, valInField );
      }
      else if ( ( type == typeof( float ) || type == typeof( double ) ) && wrapper.CanRead() ) {
        float valInField = type == typeof( double ) ? Convert.ToSingle( wrapper.Get<double>() ) : wrapper.Get<float>();
        FloatSliderInInspector slider = wrapper.GetAttribute<FloatSliderInInspector>();
        if ( slider != null )
          value = EditorGUILayout.Slider( MakeLabel( wrapper.Member ), valInField, slider.Min, slider.Max );
        else {
          value = EditorGUILayout.FloatField( MakeLabel( wrapper.Member ), valInField, skin.textField );

          // I can't remember why I tested this approach.
          //GUILayout.BeginHorizontal();
          //{
          //  GUILayout.Label( MakeLabel( wrapper.Member ) );
          //  value = EditorGUILayout.FloatField( valInField, skin.textField );
          //}
          //GUILayout.EndHorizontal();
        }
      }
      else if ( type == typeof( int ) && wrapper.CanRead() ) {
        int valInField = wrapper.Get<int>();
        value = EditorGUILayout.IntField( MakeLabel( wrapper.Member ), valInField, skin.textField );
      }
      else if ( type == typeof( bool ) && wrapper.CanRead() ) {
        bool valInField = wrapper.Get<bool>();
        value = Utils.GUI.Toggle( MakeLabel( wrapper.Member ), valInField, skin.button, skin.label );
                                   
      }
      else if ( type == typeof( Color ) && wrapper.CanRead() ) {
        Color valInField = wrapper.Get<Color>();
        value = EditorGUILayout.ColorField( MakeLabel( wrapper.Member ), valInField );
      }
      else if ( type == typeof( DefaultAndUserValueFloat ) && wrapper.CanRead() ) {
        DefaultAndUserValueFloat valInField = wrapper.Get<DefaultAndUserValueFloat>();

        float newValue = Utils.GUI.HandleDefaultAndUserValue( wrapper.Member.Name, valInField, skin );
        if ( wrapper.IsValid( newValue ) ) {
          if ( !valInField.UseDefault )
            valInField.Value = newValue;
          value = valInField;
        }
      }
      else if ( type == typeof( DefaultAndUserValueVector3 ) && wrapper.CanRead() ) {
        DefaultAndUserValueVector3 valInField = wrapper.Get<DefaultAndUserValueVector3>();

        Vector3 newValue = Utils.GUI.HandleDefaultAndUserValue( wrapper.Member.Name, valInField, skin );
        if ( wrapper.IsValid( newValue ) ) {
          if ( !valInField.UseDefault )
            valInField.Value = newValue;
          value = valInField;
        }
      }
      else if ( type == typeof( RangeReal ) ) {
        RangeReal valInField = wrapper.Get<RangeReal>();

        GUILayout.BeginHorizontal();
        {
          GUILayout.Label( MakeLabel( wrapper.Member ), skin.label );
          valInField.Min = EditorGUILayout.FloatField( "", (float)valInField.Min, skin.textField, GUILayout.MaxWidth( 64 ) );
          valInField.Max = EditorGUILayout.FloatField( "", (float)valInField.Max, skin.textField, GUILayout.MaxWidth( 64 ) );
        }
        GUILayout.EndHorizontal();

        if ( valInField.Min > valInField.Max )
          valInField.Min = valInField.Max;

        value = valInField;
      }
      else if ( type == typeof( string ) && wrapper.CanRead() ) {
        value = EditorGUILayout.TextField( MakeLabel( wrapper.Member ), wrapper.Get<string>(), skin.textField );
      }
      else if ( type.IsEnum && type.IsVisible && wrapper.CanRead() ) {
        Enum valInField = wrapper.Get<System.Enum>();
        value = EditorGUILayout.EnumPopup( MakeLabel( wrapper.Member ), valInField, skin.button );
      }
      else if ( type.IsArray && wrapper.CanRead() ) {
        Array array = wrapper.Get<Array>();
        if ( array.Length == 0 ) {
          GUILayout.BeginHorizontal();
          {
            GUILayout.Label( MakeLabel( wrapper.Member ), skin.label );
            GUILayout.Label( Utils.GUI.MakeLabel( "Empty array", true ) );
          }
          GUILayout.EndHorizontal();
        }
        else {
          Utils.GUI.Separator();
          using ( new Utils.GUI.Indent( 12 ) )
            foreach ( object obj in wrapper.Get<Array>() )
              DrawMembersGUI( obj, target, skin );
          Utils.GUI.Separator();
        }
      }
      else if ( type.IsGenericType && type.GetGenericTypeDefinition() == typeof( List<> ) && wrapper.CanRead() ) {
        HandleList( wrapper, target, skin );
      }
      else if ( type == typeof( Frame ) && wrapper.CanRead() ) {
        Frame frame = wrapper.Get<Frame>();
        Utils.GUI.HandleFrame( frame, skin );
      }
      else if ( ( type.BaseType == typeof( ScriptAsset ) || type.BaseType == typeof( UnityEngine.Object ) || type.BaseType == typeof( ScriptComponent ) ) && wrapper.CanRead() ) {
        bool allowSceneObject         = type == typeof( GameObject ) ||
                                        type.BaseType == typeof( ScriptComponent );
        UnityEngine.Object valInField = wrapper.Get<UnityEngine.Object>();
        bool recursiveEditing         = wrapper.HasAttribute<AllowRecursiveEditing>();

        if ( recursiveEditing ) {
          var foldoutData = EditorData.Instance.GetData( target as UnityEngine.Object, wrapper.Member.Name );

          GUILayout.BeginHorizontal();
          {
            GUI.enabled = valInField != null;
            foldoutData.Bool = GUILayout.Button( Utils.GUI.MakeLabel( foldoutData.Bool ? "-" : "+" ), skin.button, new GUILayoutOption[] { GUILayout.Width( 20 ), GUILayout.Height( 14 ) } ) ? !foldoutData.Bool : valInField != null && foldoutData.Bool;
            GUI.enabled = true;
            value = EditorGUILayout.ObjectField( MakeLabel( wrapper.Member ), valInField, type, allowSceneObject, new GUILayoutOption[] { } );
          }
          GUILayout.EndHorizontal();

          if ( GUILayoutUtility.GetLastRect().Contains( Event.current.mousePosition ) && Event.current.type == EventType.MouseDown && Event.current.button == 0 ) {
            foldoutData.Bool = !foldoutData.Bool;
            GUIUtility.ExitGUI();
          }

          if ( foldoutData.Bool ) {
            using ( new Utils.GUI.Indent( 12 ) ) {
              Utils.GUI.Separator();

              GUILayout.Space( 6 );

              GUILayout.Label( Utils.GUI.MakeLabel( "Changes made to this object will affect all objects referencing this asset.", Color.Lerp( Color.red, Color.white, 0.25f ), true ), new GUIStyle( skin.textArea ) { alignment = TextAnchor.MiddleCenter } );

              GUILayout.Space( 6 );

              var updateMethod = typeof( BaseEditor<> ).MakeGenericType( typeof( T ) ).GetMethod( "Update", BindingFlags.Public | BindingFlags.Static );
              updateMethod.Invoke( null, new object[] { value, target, skin } );

              Utils.GUI.Separator();
            }
          }
        }
        else {
          value = EditorGUILayout.ObjectField( MakeLabel( wrapper.Member ), valInField, type, allowSceneObject, new GUILayoutOption[] { } );
        }

        //if ( value != null && wrapper.HasAttribute<AllowRecursiveEditing>() ) {
        //  using ( new Utils.GUI.Indent( 12 ) ) {
        //    if ( Utils.GUI.Foldout( EditorData.Instance.GetData( target as UnityEngine.Object, wrapper.Member.Name ), MakeLabel( wrapper.Member ), skin ) ) {
        //      var updateMethod = typeof( BaseEditor<> ).MakeGenericType( typeof( T ) ).GetMethod( "Update", BindingFlags.Public | BindingFlags.Static );

        //      Utils.GUI.Separator();
        //      using ( new Utils.GUI.Indent( 12 ) )
        //        updateMethod.Invoke( null, new object[] { value, target, skin } );
        //      Utils.GUI.Separator();
        //    }
        //  }
        //}

        isNullable = true;
      }
      else if ( type.IsClass && wrapper.CanRead() ) {
      }

      return GUI.changed &&
             ( value != null || isNullable ) &&
             wrapper.ConditionalSet( value );
    }

    public static bool HandleCollisionGroupEntryPair( CollisionGroupEntryPair collisionGroupPair, GUISkin skin )
    {
      if ( collisionGroupPair == null )
        return false;

      GUILayout.BeginHorizontal();
      {
        collisionGroupPair.First.Tag = GUILayout.TextField( collisionGroupPair.First.Tag, skin.textField, GUILayout.Height( 19 ) );
        collisionGroupPair.Second.Tag = GUILayout.TextField( collisionGroupPair.Second.Tag, skin.textField, GUILayout.Height( 19 ) );
      }
      GUILayout.EndHorizontal();

      return true;
    }

    public static void HandleList( InvokeWrapper wrapper, T target, GUISkin skin )
    {
      System.Collections.IList list = wrapper.Get<System.Collections.IList>();
      HandleList( list, MakeLabel( wrapper.Member ), target, skin );
    }

    public static void HandleList( System.Collections.IList list, GUIContent label, T target, GUISkin skin )
    {
      if ( Utils.GUI.Foldout( EditorData.Instance.GetData( target as UnityEngine.Object, label.text ), label, skin ) ) {
        object insertElementBefore = null;
        object insertElementAfter = null;
        object eraseElement = null;
        using ( new Utils.GUI.Indent( 12 ) ) {
          foreach ( var obj in list ) {
            Utils.GUI.Separator();
            using ( new Utils.GUI.Indent( 12 ) ) {
              GUILayout.BeginHorizontal();
              {
                GUILayout.BeginVertical();
                {
                  DrawMembersGUI( obj, target, skin );
                }
                GUILayout.EndHorizontal();

                using ( Tools.WireTool.NodeListButtonColor ) {
                  if ( GUILayout.Button( Utils.GUI.MakeLabel( Utils.GUI.Symbols.ListInsertElementBefore.ToString(), false, "Insert new element before this" ), skin.button, new GUILayoutOption[] { GUILayout.Width( 26 ), GUILayout.Height( 18 ) } ) )
                    insertElementBefore = obj;
                  if ( GUILayout.Button( Utils.GUI.MakeLabel( Utils.GUI.Symbols.ListInsertElementAfter.ToString(), false, "Insert new element after this" ), skin.button, new GUILayoutOption[] { GUILayout.Width( 26 ), GUILayout.Height( 18 ) } ) )
                    insertElementAfter = obj;
                  if ( GUILayout.Button( Utils.GUI.MakeLabel( Utils.GUI.Symbols.ListEraseElement.ToString(), false, "Erase this element" ), skin.button, new GUILayoutOption[] { GUILayout.Width( 26 ), GUILayout.Height( 18 ) } ) )
                    eraseElement = obj;
                }
              }
              GUILayout.EndHorizontal();
            }
          }

          if ( list.Count == 0 )
            GUILayout.Label( Utils.GUI.MakeLabel( "Empty", true ) );
          else
            Utils.GUI.Separator();
        }

        bool addElementToList = false;
        GUILayout.BeginHorizontal();
        {
          GUILayout.FlexibleSpace();
          using ( Tools.WireTool.NodeListButtonColor )
            addElementToList = GUILayout.Button( Utils.GUI.MakeLabel( Utils.GUI.Symbols.ListInsertElementAfter.ToString(), false, "Add new element to list" ), skin.button, new GUILayoutOption[] { GUILayout.Width( 26 ), GUILayout.Height( 18 ) } );
        }
        GUILayout.EndHorizontal();

        object newObject = null;
        if ( addElementToList || insertElementBefore != null || insertElementAfter != null )
          newObject = Activator.CreateInstance( list.GetType().GetGenericArguments()[ 0 ], new object[] { } );

        if ( eraseElement != null )
          list.Remove( eraseElement );
        else if ( newObject != null ) {
          if ( addElementToList || ( list.Count > 0 && insertElementAfter != null && insertElementAfter == list[ list.Count - 1 ] ) )
            list.Add( newObject );
          else if ( insertElementAfter != null )
            list.Insert( list.IndexOf( insertElementAfter ) + 1, newObject );
          else if ( insertElementBefore != null )
            list.Insert( list.IndexOf( insertElementBefore ), newObject );
        }

        if ( eraseElement != null || newObject != null )
          EditorUtility.SetDirty( target as UnityEngine.Object );
      }
    }

    public static GUIContent MakeLabel( MemberInfo field )
    {
      GUIContent guiContent = new GUIContent();

      guiContent.text = field.Name.SplitCamelCase();
      object[] descriptions = field.GetCustomAttributes( typeof( DescriptionAttribute ), true );
      if ( descriptions.Length > 0 )
        guiContent.tooltip = ( descriptions[ 0 ] as DescriptionAttribute ).Description;

      return guiContent;
    }
  }
}
