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
  public class BaseEditor<T> : UnityEditor.Editor where T : UnityEngine.Object
  {
    #region Internal classes
    private abstract class InvokeWrapper
    {
      protected object m_obj = null;

      protected InvokeWrapper( object obj, MemberInfo member )
      {
        m_obj  = obj;
        Member = member;
      }

      public MemberInfo Member { get; private set; }

      public U GetAttribute<U>() where U : System.Attribute
      {
        object[] attributes = Member.GetCustomAttributes( typeof( U ), false );
        return attributes.Length > 0 ? attributes[ 0 ] as U : null;
      }

      public bool IsValid( object value )
      {
        object[] clampAboveZeroAttributes = Member.GetCustomAttributes( typeof( ClampAboveZeroInInspector ), false );
        if ( clampAboveZeroAttributes.Length > 0 )
          return ( clampAboveZeroAttributes[ 0 ] as ClampAboveZeroInInspector ).IsValid( value );
        return true;
      }

      /// <summary>
      /// Checks if the property can read values, i.e., has a getter. This is
      /// always true for public fields.
      /// </summary>
      /// <returns></returns>
      public virtual bool CanRead() { return true; }

      /// <summary>
      /// Checks if the property can write values, i.e., has a setter. This
      /// is always true for public fields.
      /// </summary>
      /// <returns></returns>
      public virtual bool CanWrite() { return true; }
      
      /// <summary>
      /// Returns the type of the field or property.
      /// </summary>
      /// <returns>Type of the field of property.</returns>
      public abstract Type GetContainingType();

      /// <summary>
      /// Get current value. Note that this will throw if e.g., a property only
      /// has a setter.
      /// </summary>
      /// <typeparam name="U">Type, e.g., bool, Vector3, etc..</typeparam>
      /// <returns>Current value.</returns>
      public abstract U Get<U>();

      /// <summary>
      /// Invoke set method if exist and the input is valid.
      /// </summary>
      /// <param name="value">Value to set.</param>
      /// <returns>true if set method was called with new value.</returns>
      public abstract bool ConditionalSet( object value );
    }

    /// <summary>
    /// Wrapper class for editable fields.
    /// </summary>
    private class FieldWrapper : InvokeWrapper
    {
      public FieldInfo Field { get { return (FieldInfo)Member; } }

      public FieldWrapper( object obj, FieldInfo fieldInfo )
        : base( obj, fieldInfo ) { }

      public override Type GetContainingType() { return Field.FieldType; }
      public override U Get<U>() { return (U)Field.GetValue( m_obj ); }
      public override bool ConditionalSet( object value )
      {
        if ( Field.IsLiteral || !IsValid( value ) )
          return false;
        Field.SetValue( m_obj, value );
        return true;
      }
    }

    /// <summary>
    /// Wrapper class for editable properties.
    /// </summary>
    private class PropertyWrapper : InvokeWrapper
    {
      public PropertyInfo Property { get { return (PropertyInfo)Member; } }

      public PropertyWrapper( object obj, PropertyInfo propertyInfo )
        : base( obj, propertyInfo ) { }

      public override bool CanRead() { return Property.GetGetMethod() != null; }
      public override bool CanWrite() { return Property.GetSetMethod() != null; }

      public override Type GetContainingType() { return Property.PropertyType; }
      public override U Get<U>() { return (U)Property.GetValue( m_obj, null ); }
      public override bool ConditionalSet( object value )
      {
        if ( Property.GetSetMethod() == null || !IsValid( value ) )
          return false;
        Property.SetValue( m_obj, value, null );
        return true;
      }
    }
    #endregion

    public override void OnInspectorGUI()
    {
      T data = target as T;

      DrawDefaultInspectors( (object)data, data );
    }

    public void OnEnable()
    {
      // It's possible to detect when this editor/object becomes selected.
      //if ( Application.isEditor && target != null )
      //  Debug.Log( "Create!" );
    }

    public void OnDestroy()
    {
      // It's possible to detect when this editor/object is being deleted
      // e.g., when the user presses delete.
      //if ( Application.isEditor && target == null )
      //  Debug.Log( "DESTROY" );
    }

    public static bool Update( T obj )
    {
      return DrawDefaultInspectors( obj, obj );
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

    private static bool DrawDefaultInspectors( object obj, T target, bool indentInEditor = true )
    {
      if ( obj == null )
        return false;

      if ( obj.GetType() == typeof( CollisionGroupEntryPair ) )
        return HandleCollisionGroupEntryPair( obj as CollisionGroupEntryPair );
      else if ( obj.GetType() == typeof( ContactMaterialEntry ) )
        return HandleContactMaterialEntry( obj as ContactMaterialEntry );

      var fields = from fieldInfo in obj.GetType().GetFields( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static )
                   where
                     ShouldBeShownInInspector( fieldInfo )
                   select fieldInfo;
      var properties = from propertyInfo in obj.GetType().GetProperties( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static )
                       where
                         ShouldBeShownInInspector( propertyInfo )
                       select propertyInfo;
      var methods = from methodInfo in obj.GetType().GetMethods( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static )
                    where
                      ShouldBeShownInInspector( methodInfo )
                    select methodInfo;

      bool changed = false;

      fields.ToList().ForEach( fieldInfo => changed = HandleType( new FieldWrapper( obj, fieldInfo ), target ) || changed );
      properties.ToList().ForEach( propertyInfo => changed = HandleType( new PropertyWrapper( obj, propertyInfo ), target ) || changed );
      methods.ToList().ForEach( methodInfo => changed = HandleMethod( methodInfo, target ) || changed );

      return changed;
    }

    private static bool HandleMethod( MethodInfo methodInfo, T target )
    {
      if ( methodInfo == null )
        return false;

      bool invoked = false;
      if ( GUILayout.Button( MakeLabel( methodInfo ), Utils.GUIHelper.EditorSkin.button, new GUILayoutOption[]{} ) ) {
        methodInfo.Invoke( target, new object[] { } );
        invoked = true;
      }

      return invoked;
    }

    private static bool HandleType( InvokeWrapper wrapper, T target )
    {
      Undo.RecordObject( target, "" );

      object value = null;
      bool isNullable = false;
      Type type = wrapper.GetContainingType();
      if ( type == typeof( Vector4 ) && wrapper.CanRead() ) {
        Vector4 valInField = wrapper.Get<Vector4>();
        value = EditorGUILayout.Vector4Field( MakeLabel( wrapper.Member ).text, valInField, null );
      }
      else if ( type == typeof( Vector3 ) && wrapper.CanRead() ) {
        Vector3 valInField = wrapper.Get<Vector3>();
        value = EditorGUILayout.Vector3Field( MakeLabel( wrapper.Member ).text, valInField, null );
      }
      else if ( type == typeof( Vector2 ) && wrapper.CanRead() ) {
        Vector2 valInField = wrapper.Get<Vector2>();
        value = EditorGUILayout.Vector2Field( MakeLabel( wrapper.Member ).text, valInField, null );
      }
      else if ( ( type == typeof( float ) || type == typeof( double ) ) && wrapper.CanRead() ) {
        float valInField = type == typeof( double ) ? Convert.ToSingle( wrapper.Get<double>() ) : wrapper.Get<float>();
        FloatSliderInInspector slider = wrapper.GetAttribute<FloatSliderInInspector>();
        if ( slider != null )
          value = EditorGUILayout.Slider( MakeLabel( wrapper.Member ), valInField, slider.Min, slider.Max, new GUILayoutOption[] { } );
        else
          value = EditorGUILayout.FloatField( MakeLabel( wrapper.Member ).text, valInField );
      }
      else if ( type == typeof( int ) && wrapper.CanRead() ) {
        int valInField = wrapper.Get<int>();
        value = EditorGUILayout.IntField( MakeLabel( wrapper.Member ).text, valInField );
      }
      else if ( type == typeof( bool ) && wrapper.CanRead() ) {
        bool valInField = wrapper.Get<bool>();
        value = EditorGUILayout.Toggle( MakeLabel( wrapper.Member ).text, valInField );
      }
      else if ( type == typeof( Color ) && wrapper.CanRead() ) {
        Color valInField = wrapper.Get<Color>();
        value = EditorGUILayout.ColorField( MakeLabel( wrapper.Member ).text, valInField );
      }
      else if ( type == typeof( DefaultAndUserValueFloat ) && wrapper.CanRead() ) {
        DefaultAndUserValueFloat valInField = wrapper.Get<DefaultAndUserValueFloat>();

        float newValue = HandleDefaultAndUserValue<float>( wrapper.Member.Name, valInField );
        if ( wrapper.IsValid( newValue ) ) {
          valInField.Value = newValue;
          value = valInField;
        }
      }
      else if ( type == typeof( DefaultAndUserValueVector3 ) && wrapper.CanRead() ) {
        DefaultAndUserValueVector3 valInField = wrapper.Get<DefaultAndUserValueVector3>();

        Vector3 newValue = HandleDefaultAndUserValue<Vector3>( wrapper.Member.Name, valInField );
        if ( wrapper.IsValid( newValue ) ) {
          valInField.Value = newValue;
          value = valInField;
        }
      }
      else if ( type == typeof( CollisionGroupEntry ) && wrapper.CanRead() ) {
        CollisionGroupEntry valInField = wrapper.Get<CollisionGroupEntry>();
        valInField.Tag = EditorGUILayout.TextField( MakeLabel( wrapper.Member ), valInField.Tag, new GUILayoutOption[] { } );
      }
      else if ( type == typeof( RangeReal ) ) {
        RangeReal valInField = wrapper.Get<RangeReal>();
        EditorGUILayout.BeginHorizontal();
        valInField.Min = EditorGUILayout.FloatField( "Min", (float)valInField.Min );
        valInField.Max = EditorGUILayout.FloatField( "Max", (float)valInField.Max );
        EditorGUILayout.EndHorizontal();
        value = valInField;
      }
      else if ( type == typeof( string ) && wrapper.CanRead() ) {
        value = EditorGUILayout.TextField( MakeLabel( wrapper.Member ), wrapper.Get<string>(), new GUILayoutOption[] { } );
      }
      else if ( type == typeof( System.String ) && wrapper.CanRead() ) {
        Debug.Log( "System: " + wrapper.Get<System.String>() );
      }
      else if ( type.IsEnum && type.IsVisible && wrapper.CanRead() ) {
        Enum valInField = wrapper.Get<System.Enum>();
        value = EditorGUILayout.EnumPopup( MakeLabel( wrapper.Member ), valInField, new GUILayoutOption[] { } );
      }
      else if ( type.IsArray && wrapper.CanRead() ) {
        HandleArray( wrapper.Get<Array>(), target );
      }
      else if ( type.IsGenericType && type.GetGenericTypeDefinition() == typeof( List<> ) && wrapper.CanRead() ) {
        HandleList( wrapper, target );
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
      //else if ( type.BaseType == typeof( AgXUnity.Deformable1DParameter ) ) {
      //  AgXUnity.Deformable1DParameter parameters = wrapper.Get<AgXUnity.Deformable1DParameter>();
      //  if ( SetBool( parameters, EditorGUILayout.Foldout( GetOrCreateBool( parameters ), wrapper.Member.Name.SplitCamelCase() ) ) ) {
      //    foreach ( AgXUnity.Deformable1DParameter.Direction dir in AgXUnity.Deformable1DParameter.Directions )
      //      parameters[ dir ] = EditorGUILayout.FloatField( dir.ToString().SplitCamelCase(), parameters[ dir ] );
      //  }
      //}
      else if ( type.IsClass && wrapper.CanRead() ) {
        // Ignore these? We will end up here with components of components which has
        // GenerateCustomEditor attribute (the parent), but THAT child component will
        // be called to this custom editor as well if desired through the custom attribute.
      }

      if ( GUI.changed && ( value != null || isNullable ) ) {
        // Assets aren't saved with the project if they aren't flagged
        // as dirty. Also true for AssetDatabase.SaveAssets.
        if ( typeof( T ).BaseType == typeof( ScriptAsset ) )
          EditorUtility.SetDirty( target );

        return wrapper.ConditionalSet( value );
      }

      return false;
    }
    
    private static ValueT HandleDefaultAndUserValue<ValueT>( string name, DefaultAndUserValue<ValueT> valInField ) where ValueT : struct
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

      Utils.GUIHelper.Separator();
      {
        EditorGUILayout.BeginHorizontal();
        {
          var textDim = GUI.skin.label.CalcSize( new GUIContent( name.SplitCamelCase() ) );
          GUILayout.Label( name.SplitCamelCase(), GUILayout.MaxWidth( textDim.x ) );
          int gridSelection = GUI.SelectionGrid( EditorGUILayout.GetControlRect( false ), Convert.ToInt32( valInField.UseDefault ), new string[] { "Default", "User specified" }, 2 );
          valInField.UseDefault = gridSelection == 0;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.BeginDisabledGroup( valInField.UseDefault );
        {
          newValue = ( ValueT )method.Invoke( null, new object[] { "", valInField.Value, new GUILayoutOption[] { } } );
        }
        EditorGUI.EndDisabledGroup();
      }
      Utils.GUIHelper.Separator();

      return newValue;
    }

    private static bool GetOrCreateBool( object obj, bool defaultValue = false )
    {
      string key = obj.GetHashCode().ToString();
      if ( EditorPrefs.HasKey( key ) )
        return EditorPrefs.GetBool( key );
      return SetBool( obj, defaultValue );
    }

    private static bool SetBool( object obj, bool value )
    {
      string key = obj.GetHashCode().ToString();
      EditorPrefs.SetBool( key, value );
      return value;
    }

    private static bool HandleCollisionGroupEntryPair( CollisionGroupEntryPair collisionGroupPair )
    {
      if ( collisionGroupPair == null )
        return false;

      collisionGroupPair.First.Tag = EditorGUILayout.TextField( "", collisionGroupPair.First.Tag, GUILayout.MaxWidth( 110 ) );
      collisionGroupPair.Second.Tag = EditorGUILayout.TextField( "", collisionGroupPair.Second.Tag, GUILayout.MaxWidth( 110 ) );

      return true;
    }

    private static bool HandleContactMaterialEntry( ContactMaterialEntry contactMaterialEntry )
    {
      if ( contactMaterialEntry == null )
        return false;

      contactMaterialEntry.ContactMaterial = EditorGUILayout.ObjectField( contactMaterialEntry.ContactMaterial, typeof( ContactMaterial ), false ) as ContactMaterial;

      return true;
    }

    private static void HandleArray( Array array, T target )
    {
      if ( array == null )
        return;

      if ( array.GetType().GetElementType() == typeof( ConstraintRowData ) ) {
        foreach ( ConstraintRowData crd in array ) {
          if ( SetBool( crd, EditorGUILayout.Foldout( GetOrCreateBool( crd ), crd.DefinitionString ) ) ) {
            bool changed = DrawDefaultInspectors( crd, target, false );
            if ( changed )
              ( target as ElementaryConstraint ).OnRowDataChanged();
          }
        }
      }
      else {
        foreach ( object obj in array )
          DrawDefaultInspectors( obj, target, false );
      }
    }

    private static void HandleList( InvokeWrapper wrapper, T target )
    {
      System.Collections.IList list = wrapper.Get<System.Collections.IList>();
      if ( SetBool( list, EditorGUILayout.Foldout( GetOrCreateBool( list ), MakeLabel( wrapper.Member ) ) ) ) {
        List<object> objectsToRemove = new List<object>();
        foreach ( object obj in list ) {
          EditorGUILayout.BeginHorizontal();
          DrawDefaultInspectors( obj, target, false );
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

    private static GUIContent MakeLabel( MemberInfo field )
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
