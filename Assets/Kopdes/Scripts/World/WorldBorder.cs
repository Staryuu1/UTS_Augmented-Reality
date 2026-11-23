using UnityEngine;

public class WorldBorder : MonoBehaviour
{
    public const float HalfSize = 65f;
    public bool NearEdge(Vector3 p) => Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z)) > HalfSize - 4;
    public Vector3 Contain(Vector3 p, float margin)
    {
        p.x = Mathf.Clamp(p.x, -HalfSize + margin, HalfSize - margin);
        p.z = Mathf.Clamp(p.z, -HalfSize + margin, HalfSize - margin);
        return p;
    }
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1,.65f,.1f,.6f);
        Gizmos.DrawWireCube(new Vector3(0,100,0),new Vector3(HalfSize*2,240,HalfSize*2));
    }
}
