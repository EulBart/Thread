using UnityEngine;
using Random = System.Random;

public class RandomizeJob : Job<Vector4>
{
    private readonly float max;
    public RandomizeJob(float max, Container<Vector4> source) : base(source)
    {
        this.max = max;
    }
    Random r = new Random();

    private float RandomValue
    {
        get {
            return 0.5f - (float)r.NextDouble();
        }
    }

    private void Randomize(ref Vector4 e)
    {
        e.Set(max * RandomValue, max * RandomValue, max * RandomValue, RandomValue);
    }

    protected override ExecuteDelegate callback
    {
        get { return Randomize; }
    }
}