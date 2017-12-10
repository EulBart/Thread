using System;
using UnityEngine;
using NUnit.Framework;

[Category("Observer")]
public class ObserverTests {

    const double delta = 0.001;
    

    [Test]
    public void CTN_0_0_Equals_1_0_0()
    {
        Vector3 position = Observer.CoordinatesToNormal(0,0);
        Assert.AreEqual(1,position.x,  delta);
        Assert.AreEqual(0,position.y,  delta);
        Assert.AreEqual(0,position.z,  delta);
    }

    private static void ForAllLongitudes(Action<float> angleAction)
    {
        for(int longitude = -175; longitude < 180; longitude+=5)
        {
            angleAction(longitude);
        }
    }

    private static void ForAllLatitudes(Action<float> angleAction)
    {
        for(int latitude = -85; latitude <= 85; latitude += 5)
        {
            angleAction(latitude);
        }
    }



    [Test]
    public void CTN_Any_90_Equals_0_1_0()
    {
        ForAllLongitudes( longitude =>
        {
            Vector3 position = Observer.CoordinatesToNormal(longitude, 90);
            Assert.AreEqual(0, position.x,  delta);
            Assert.AreEqual(1, position.y,  delta);
            Assert.AreEqual(0, position.z,  delta);
        });
    }

    [Test]
    public void NTC_MagnitudeIndependant()
    {
        Vector3 v = new Vector3(1,4,2);
        Vector2 c0 = Observer.NormalToCoordinates(v);
        v.Normalize();
        Vector2 c1 = Observer.NormalToCoordinates(v);
        Assert.That(c0.magnitude, Is.GreaterThan(0));
        //Assert.AreEqual(c0.x, c1.x, delta);
        //Assert.AreEqual(c0.y, c1.y, delta);
        Assert.That(c0,Is.EqualTo(c1));
    }

    [Test]
    public void NTC_And_CTN_AreInverse()
    {
        ForAllLongitudes( longitude =>
        ForAllLatitudes( latitude =>
        {
//            string msg = "L:" + longitude + " l:" + latitude;
//            LogAssert.Expect(LogType.Log, msg);
            Vector3 v = Observer.CoordinatesToNormal(longitude, latitude);
            Vector2 c = Observer.NormalToCoordinates(v);
//            Debug.Log(msg);
            //Assert.That(c, Is.EqualTo(new Vector2(longitude, latitude)));
            Assert.AreEqual(longitude, c.x,  delta);
            Assert.AreEqual(latitude, c.y, delta);
        }));
    }
}
