using System;
using UnityEngine;

namespace AgXUnity
{
  [DoNotGenerateCustomEditor]
  public class CableProperty : ScriptableObject
  {
    public static CableProperty Create( CableProperties.Direction dir, Action<CableProperties.Direction> onValueChanged )
    {
      CableProperty property = CreateInstance<CableProperty>();
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

    public static CableProperties Create()
    {
      CableProperties properties = Create<CableProperties>();
      properties.hideFlags = HideFlags.HideAndDontSave;

      return properties;
    }

    [SerializeField]
    private CableProperty[] m_properties = new CableProperty[ Enum.GetValues( typeof( Direction ) ).Length ];
    public CableProperty this[ Direction dir ]
    {
      get { return m_properties[ (int)dir ]; }
    }

    public static Array Directions { get { return Enum.GetValues( typeof( Direction ) ); } }

    public override void Destroy()
    {
    }

    protected override void Construct()
    {
      foreach ( Direction dir in Directions ) {
        m_properties[ (int)dir ] = CableProperty.Create( dir, OnPropertyChanged );
        m_properties[ (int)dir ].hideFlags = HideFlags.HideAndDontSave;
      }
    }

    protected override bool Initialize()
    {
      return true;
    }

    private void OnPropertyChanged( Direction dir )
    {
      Debug.Log( dir );
    }
  }
}
