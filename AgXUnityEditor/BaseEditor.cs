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
  public class BaseEditor<T> : UnityEditor.Editor where T : class
  {
    public override void OnInspectorGUI()
    {
      DrawMembersGUI( target, target as T, CurrentSkin );
    }

    public void OnEnable()
    {
      GUISkin guiSkin = EditorGUIUtility.GetBuiltinSkin( EditorSkin.Inspector );
      guiSkin.label.richText = true;

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

    public static bool Update( T obj, GUISkin skin = null )
    {
      return DrawMembersGUI( obj, obj, skin ?? EditorGUIUtility.GetBuiltinSkin( EditorSkin.Inspector ) );
    }

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

    private static GUISkin CurrentSkin { get { return EditorGUIUtility.GetBuiltinSkin( EditorSkin.Inspector ); } }

    private static bool DrawMembersGUI( object obj, T target, GUISkin skin )
    {
      if ( obj == null )
        return false;

      if ( obj.GetType() == typeof( CollisionGroupEntryPair ) )
        return HandleCollisionGroupEntryPair( obj as CollisionGroupEntryPair );
      else if ( obj.GetType() == typeof( ContactMaterialEntry ) )
        return HandleContactMaterialEntry( obj as ContactMaterialEntry );

      if ( obj == target )
        Utils.GUI.PreTargetMembers( target, CurrentSkin );

      bool changed = false;
      InvokeWrapper[] fieldsAndProperties = InvokeWrapper.FindFieldsAndProperties( obj );
      foreach ( InvokeWrapper wrapper in fieldsAndProperties )
        if ( ShouldBeShownInInspector( wrapper.Member ) )
          changed = HandleType( wrapper, target ) || changed;

      var methods = from methodInfo in obj.GetType().GetMethods( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static )
                    where
                      ShouldBeShownInInspector( methodInfo )
                    select methodInfo;
      methods.ToList().ForEach( methodInfo => changed = HandleMethod( methodInfo, target ) || changed );

      return changed;
    }

    private static bool HandleMethod( MethodInfo methodInfo, T target )
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
      if ( GUILayout.Button( label, CurrentSkin.button, new GUILayoutOption[]{} ) ) {
        methodInfo.Invoke( target, new object[] { } );
        invoked = true;
      }

      return invoked;
    }

    public static bool HandleType( InvokeWrapper wrapper, T target )
    {
      if ( target != null && target is UnityEngine.Object )
        Undo.RecordObject( target as UnityEngine.Object, "" );

      object value = null;
      bool isNullable = false;
      Type type = wrapper.GetContainingType();
      if ( type == typeof( Vector4 ) && wrapper.CanRead() ) {
        Vector4 valInField = wrapper.Get<Vector4>();
        value = EditorGUILayout.Vector4Field( MakeLabel( wrapper.Member ).text, valInField );
      }
      else if ( type == typeof( Vector3 ) && wrapper.CanRead() ) {
        Vector3 valInField = wrapper.Get<Vector3>();
        value = EditorGUILayout.Vector3Field( MakeLabel( wrapper.Member ).text, valInField );
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
        else
          value = EditorGUILayout.FloatField( MakeLabel( wrapper.Member ), valInField, CurrentSkin.textField );
      }
      else if ( type == typeof( int ) && wrapper.CanRead() ) {
        int valInField = wrapper.Get<int>();
        value = EditorGUILayout.IntField( MakeLabel( wrapper.Member ), valInField, CurrentSkin.textField );
      }
      else if ( type == typeof( bool ) && wrapper.CanRead() ) {
        bool valInField = wrapper.Get<bool>();
        value = EditorGUILayout.Toggle( MakeLabel( wrapper.Member ), valInField, CurrentSkin.toggle );
      }
      else if ( type == typeof( Color ) && wrapper.CanRead() ) {
        Color valInField = wrapper.Get<Color>();
        value = EditorGUILayout.ColorField( MakeLabel( wrapper.Member ), valInField );
      }
      else if ( type == typeof( DefaultAndUserValueFloat ) && wrapper.CanRead() ) {
        DefaultAndUserValueFloat valInField = wrapper.Get<DefaultAndUserValueFloat>();

        float newValue = Utils.GUI.HandleDefaultAndUserValue( wrapper.Member.Name, valInField, CurrentSkin );
        if ( wrapper.IsValid( newValue ) ) {
          if ( !valInField.UseDefault )
            valInField.Value = newValue;
          value = valInField;
        }
      }
      else if ( type == typeof( DefaultAndUserValueVector3 ) && wrapper.CanRead() ) {
        DefaultAndUserValueVector3 valInField = wrapper.Get<DefaultAndUserValueVector3>();

        Vector3 newValue = Utils.GUI.HandleDefaultAndUserValue( wrapper.Member.Name, valInField, CurrentSkin );
        if ( wrapper.IsValid( newValue ) ) {
          if ( !valInField.UseDefault )
            valInField.Value = newValue;
          value = valInField;
        }
      }
      else if ( type == typeof( CollisionGroupEntry ) && wrapper.CanRead() ) {
        CollisionGroupEntry valInField = wrapper.Get<CollisionGroupEntry>();
        valInField.Tag = EditorGUILayout.TextField( MakeLabel( wrapper.Member ), valInField.Tag, CurrentSkin.textField );
      }
      else if ( type == typeof( RangeReal ) ) {
        RangeReal valInField = wrapper.Get<RangeReal>();

        EditorGUILayout.BeginHorizontal();
        {
          GUILayout.Label( MakeLabel( wrapper.Member ), CurrentSkin.label );
          valInField.Min = EditorGUILayout.FloatField( "", (float)valInField.Min, CurrentSkin.textField, GUILayout.MaxWidth( 64 ) );
          valInField.Max = EditorGUILayout.FloatField( "", (float)valInField.Max, CurrentSkin.textField, GUILayout.MaxWidth( 64 ) );
        }
        EditorGUILayout.EndHorizontal();

        if ( valInField.Min > valInField.Max )
          valInField.Min = valInField.Max;

        value = valInField;
      }
      else if ( type == typeof( string ) && wrapper.CanRead() ) {
        value = EditorGUILayout.TextField( MakeLabel( wrapper.Member ), wrapper.Get<string>(), CurrentSkin.textField );
      }
      else if ( type == typeof( System.String ) && wrapper.CanRead() ) {
        Debug.Log( "System: " + wrapper.Get<System.String>() );
      }
      else if ( type.IsEnum && type.IsVisible && wrapper.CanRead() ) {
        Enum valInField = wrapper.Get<System.Enum>();
        value = EditorGUILayout.EnumPopup( MakeLabel( wrapper.Member ), valInField, CurrentSkin.button );
      }
      else if ( type.IsArray && wrapper.CanRead() ) {
        HandleArray( wrapper.Get<Array>(), target );
      }
      else if ( type.IsGenericType && type.GetGenericTypeDefinition() == typeof( List<> ) && wrapper.CanRead() ) {
        HandleList( wrapper, target );
      }
      else if ( type == typeof( Frame ) && wrapper.CanRead() ) {
        Frame frame = wrapper.Get<Frame>();
        Utils.GUI.HandleFrame( frame, CurrentSkin );
      }
      else if ( ( type.BaseType == typeof( ScriptAsset ) || type.BaseType == typeof( UnityEngine.Object ) || type.BaseType == typeof( ScriptComponent ) ) && wrapper.CanRead() ) {
        bool allowSceneObject = type == typeof( GameObject ) ||
                                type.BaseType == typeof( ScriptComponent );

        UnityEngine.Object valInField = wrapper.Get<UnityEngine.Object>();

        EditorGUILayout.BeginHorizontal();
        value = EditorGUILayout.ObjectField( MakeLabel( wrapper.Member ), valInField, type, allowSceneObject, new GUILayoutOption[] { } );
        EditorGUILayout.EndHorizontal();

        isNullable = true;
      }
      else if ( type.IsClass && wrapper.CanRead() ) {
      }

      if ( GUI.changed && ( value != null || isNullable ) ) {
        // Assets aren't saved with the project if they aren't flagged
        // as dirty. Also true for AssetDatabase.SaveAssets.
        if ( typeof( T ).BaseType == typeof( ScriptAsset ) )
          EditorUtility.SetDirty( target as UnityEngine.Object );

        return wrapper.ConditionalSet( value );
      }

      return false;
    }

    public static bool HandleCollisionGroupEntryPair( CollisionGroupEntryPair collisionGroupPair )
    {
      if ( collisionGroupPair == null )
        return false;

      EditorGUILayout.BeginHorizontal();
      collisionGroupPair.First.Tag = EditorGUILayout.TextField( "", collisionGroupPair.First.Tag, CurrentSkin.textField, GUILayout.MaxWidth( 110 ) );
      collisionGroupPair.Second.Tag = EditorGUILayout.TextField( "", collisionGroupPair.Second.Tag, CurrentSkin.textField, GUILayout.MaxWidth( 110 ) );
      EditorGUILayout.EndHorizontal();

      return true;
    }

    public static bool HandleContactMaterialEntry( ContactMaterialEntry contactMaterialEntry )
    {
      if ( contactMaterialEntry == null )
        return false;

      contactMaterialEntry.ContactMaterial = EditorGUILayout.ObjectField( contactMaterialEntry.ContactMaterial, typeof( ContactMaterial ), false ) as ContactMaterial;

      return true;
    }

    public static void HandleArray( Array array, T target )
    {
      if ( array == null )
        return;

      if ( array.GetType().GetElementType() == typeof( AgXUnity.Deprecated.ConstraintRowData ) ) {
        foreach ( AgXUnity.Deprecated.ConstraintRowData crd in array ) {
          if ( Utils.GUI.Prefs.SetBool( crd, EditorGUILayout.Foldout( Utils.GUI.Prefs.GetOrCreateBool( crd ), crd.DefinitionString ) ) ) {
            bool changed = DrawMembersGUI( crd, target, CurrentSkin );
            if ( changed )
              ( target as AgXUnity.Deprecated.ElementaryConstraint ).OnRowDataChanged();
          }
        }
      }
      else {
        foreach ( object obj in array )
          DrawMembersGUI( obj, target, CurrentSkin );
      }
    }

    private static void HandleList( InvokeWrapper wrapper, T target )
    {
      System.Collections.IList list = wrapper.Get<System.Collections.IList>();
      HandleList( list, MakeLabel( wrapper.Member ), target );
    }

    public static void HandleList( System.Collections.IList list, GUIContent label, T target )
    {
      if ( Utils.GUI.Prefs.SetBool( list, EditorGUILayout.Foldout( Utils.GUI.Prefs.GetOrCreateBool( list ), label ) ) ) {
        List<object> objectsToRemove = new List<object>();
        foreach ( object obj in list ) {
          EditorGUILayout.BeginHorizontal();
          EditorGUILayout.BeginVertical();
          DrawMembersGUI( obj, target, CurrentSkin );
          EditorGUILayout.EndVertical();
          if ( GUILayout.Button( "Del", GUILayout.MaxWidth( 32 ) ) )
            objectsToRemove.Add( obj );
          EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if ( GUILayout.Button( "Add", GUILayout.ExpandWidth( false ) ) )
          list.Add( Activator.CreateInstance( list.GetType().GetGenericArguments()[ 0 ], new object[] { } ) );
        EditorGUILayout.EndHorizontal();

        foreach ( object obj in objectsToRemove )
          list.Remove( obj );
      }
    }

    public static bool IgnoreMakeLabelCalls = false;
    public static GUIContent MakeLabel( MemberInfo field )
    {
      GUIContent guiContent = new GUIContent();
      if ( IgnoreMakeLabelCalls )
        return guiContent;

      guiContent.text = field.Name.SplitCamelCase();
      object[] descriptions = field.GetCustomAttributes( typeof( DescriptionAttribute ), true );
      if ( descriptions.Length > 0 )
        guiContent.tooltip = ( descriptions[ 0 ] as DescriptionAttribute ).Description;

      return guiContent;
    }
  }
}
