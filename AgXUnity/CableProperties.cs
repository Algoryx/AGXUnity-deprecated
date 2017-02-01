using System;
using UnityEngine;

namespace AgXUnity
{
  [Serializable]
  public class CableProperty
  {
    public static CableProperty Create( CableProperties.Direction dir, Action<CableProperties.Direction> onValueChanged )
    {
      CableProperty property = new CableProperty();
      property.Direction = dir;
      property.OnValueCanged += onValueChanged;

      return property;
    }

    public Action<CableProperties.Direction> OnValueCanged = delegate { };

    [SerializeField]
    private CableProperties.Direction m_direction = CableProperties.Direction.Bend;
    public CableProperties.Direction Direction
    {
      get { return m_direction; }
      private set { m_direction = value; }
    }

    [SerializeField]
    private float m_youngsModulus = 1.0E9f;
    public float YoungsModulus
    {
      get { return m_youngsModulus; }
      set
      {
        m_youngsModulus = value;

        OnValueCanged( Direction );
      }
    }

    [SerializeField]
    private float m_yieldPoint = float.PositiveInfinity;
    public float YieldPoint
    {
      get { return m_yieldPoint; }
      set
      {
        m_yieldPoint = value;

        OnValueCanged( Direction );
      }
    }

    [SerializeField]
    private float m_damping = 2.0f / 50;
    public float Damping
    {
      get { return m_damping; }
      set
      {
        m_damping = value;

        OnValueCanged( Direction );
      }
    }
  }

  [DoNotGenerateCustomEditor]
  public class CableProperties : ScriptAsset
  {
    public enum Direction
    {
      Bend,
      Twist,
      Stretch
    }

    public static Array Directions { get { return Enum.GetValues( typeof( Direction ) ); } }

    public static agxCable.CableProperties.Direction ToNative( Direction dir )
    {
      return (agxCable.CableProperties.Direction)dir;
    }

    [SerializeField]
    private CableProperty[] m_properties = new CableProperty[ Enum.GetValues( typeof( Direction ) ).Length ];
    public CableProperty this[ Direction dir ]
    {
      get { return m_properties[ (int)dir ]; }
      private set { m_properties[ (int)dir ] = value; }
    }

    public Action<Direction> OnPropertyUpdated = delegate { };

    public bool IsListening( Cable cable )
    {
      var invocationList = OnPropertyUpdated.GetInvocationList();
      foreach ( var listener in invocationList )
        if ( cable.Equals( listener.Target ) )
          return true;

      return false;
    }

    public override void Destroy()
    {
    }

    protected override void Construct()
    {
      foreach ( Direction dir in Directions )
        this[ dir ] = CableProperty.Create( dir, OnPropertyChanged );
    }

    protected override bool Initialize()
    {
      return true;
    }

    private void OnPropertyChanged( Direction dir )
    {
      OnPropertyUpdated( dir );
    }
  }
}
