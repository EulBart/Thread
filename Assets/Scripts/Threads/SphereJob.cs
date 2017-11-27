using UnityEngine;
public class SphereJob : Job<Vector4>
{
    private float radius;
    private float f;

    public SphereJob(float radius, float f, Container<Vector4> source) : base(source)
    {
        this.radius = radius;
        this.f = f;
    }

    private void PutOnSphere(int index, ref Vector4 v)
    {
        v = Vector4.Lerp(v, v.normalized * radius, f);
    }

    protected override ExecuteDelegate callback
    {
        get { return PutOnSphere; }
    }
}
