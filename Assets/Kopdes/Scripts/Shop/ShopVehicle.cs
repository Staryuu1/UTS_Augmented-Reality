using System.Collections;
using UnityEngine;

public sealed class ShopVehicle : MonoBehaviour
{
    public Transform parkingBay;
    public Transform[] wheels;
    public GameObject rider;
    public float wheelRadius = .3f;

    const float RoadSpeed = 8f;
    const float TurnSpeed = 4.4f;
    const float ParkingSpeed = 2.6f;
    const float ReverseSpeed = 1.5f;
    const float Acceleration = 3f;
    const float Braking = 4f;
    static readonly Vector3 RoadExit = new(64f, .045f, -22f);
    float speed;

    public Vector3 Door => parkingBay.position + Vector3.right * (rider ? .72f : 1.17f);
    public Vector3 ApproachDoor => new(Door.x, Door.y, -6.8f);

    public void Spawn(float x)
    {
        transform.SetPositionAndRotation(new Vector3(x, .045f, -22f), Quaternion.Euler(0, 90, 0));
        speed = RoadSpeed;
        RiderVisible(true);
    }

    public IEnumerator PassBy() => Line(RoadExit, RoadSpeed, RoadSpeed);

    public IEnumerator Arrive()
    {
        yield return Line(new Vector3(-3.4f, .045f, -22f), RoadSpeed, TurnSpeed);
        yield return Curve(new Vector3(1.8f, .061f, -16.8f), Vector3.right, Vector3.forward, TurnSpeed, ParkingSpeed);
        float x = parkingBay.position.x;
        float side = Mathf.Sign(x);
        yield return Curve(new Vector3(1.8f + side * 2.5f, .061f, -14.4f), Vector3.forward, Vector3.right * side, ParkingSpeed, ParkingSpeed);
        yield return Line(new Vector3(x - side * 2.15f, .061f, -14.4f), ParkingSpeed, ParkingSpeed);
        yield return Curve(new Vector3(x, .061f, -12.25f), Vector3.right * side, Vector3.forward, ParkingSpeed, ParkingSpeed);
        yield return Line(parkingBay.position, ParkingSpeed, 0f);
        RiderVisible(false);
    }

    public IEnumerator Depart()
    {
        float x = parkingBay.position.x;
        float side = Mathf.Sign(x);
        RiderVisible(true);
        yield return Line(new Vector3(x, .061f, -11.85f), ReverseSpeed, ReverseSpeed, true);
        yield return Curve(new Vector3(x + side * 2.45f, .061f, -14.35f), Vector3.back, Vector3.right * side, ReverseSpeed, 0f, true);
        yield return Line(new Vector3(side * 2.8f, .061f, -14.35f), ParkingSpeed, ParkingSpeed);
        yield return Curve(new Vector3(0, .061f, -17.15f), Vector3.left * side, Vector3.back, ParkingSpeed, ParkingSpeed);
        yield return Curve(new Vector3(4.85f, .045f, -22f), Vector3.back, Vector3.right, TurnSpeed, TurnSpeed);
        yield return PassBy();
    }

    void RiderVisible(bool visible)
    {
        if (rider) rider.SetActive(visible);
    }

    IEnumerator Line(Vector3 destination, float limit, float endSpeed, bool reverse = false)
    {
        Vector3 start = transform.position;
        return Move(destination, Vector3.Lerp(start, destination, 1f / 3f), Vector3.Lerp(start, destination, 2f / 3f), limit, endSpeed, reverse);
    }

    IEnumerator Curve(Vector3 destination, Vector3 startDirection, Vector3 endDirection, float limit, float endSpeed, bool reverse = false)
    {
        float handle = Vector3.Distance(transform.position, destination) * .39f;
        return Move(destination, transform.position + startDirection * handle, destination - endDirection * handle, limit, endSpeed, reverse);
    }

    IEnumerator Move(Vector3 destination, Vector3 first, Vector3 second, float limit, float endSpeed, bool reverse)
    {
        Vector3 start = transform.position;
        // A table of distances keeps the cubic's parameter from changing the driving speed.
        const int samples = 20;
        var distances = new float[samples + 1];
        Vector3 previous = start;
        for (int i = 1; i <= samples; i++)
        {
            Vector3 point = Bezier(start, first, second, destination, (float)i / samples, out _);
            distances[i] = distances[i - 1] + Vector3.Distance(previous, point);
            previous = point;
        }

        float length = distances[samples];
        float traveled = 0;
        int next = 1;
        while (traveled < length)
        {
            float brakingSpeed = Mathf.Sqrt(endSpeed * endSpeed + 2f * Braking * (length - traveled));
            float target = Mathf.Min(limit, brakingSpeed);
            float rate = speed < target ? Acceleration : Braking;
            float newSpeed = Mathf.MoveTowards(speed, target, rate * Time.deltaTime);
            traveled = Mathf.Min(length, traveled + (speed + newSpeed) * .5f * Time.deltaTime);
            speed = newSpeed;

            while (next < samples && traveled > distances[next]) next++;
            float fraction = Mathf.InverseLerp(distances[next - 1], distances[next], traveled);
            float t = (next - 1 + fraction) / samples;
            Vector3 position = Bezier(start, first, second, destination, t, out Vector3 tangent);
            tangent.y = 0;
            float sign = reverse ? -1f : 1f;
            float wheelTurn = Vector3.Distance(transform.position, position) / wheelRadius * Mathf.Rad2Deg * sign;
            foreach (Transform wheel in wheels) wheel.Rotate(wheelTurn, 0, 0, Space.Self);
            Quaternion heading = tangent.sqrMagnitude > .00001f ? Quaternion.LookRotation(tangent * sign) : transform.rotation;
            transform.SetPositionAndRotation(position, heading);
            yield return null;
        }

        speed = endSpeed;
    }

    static Vector3 Bezier(Vector3 start, Vector3 first, Vector3 second, Vector3 end, float t, out Vector3 tangent)
    {
        Vector3 a = Vector3.Lerp(start, first, t);
        Vector3 b = Vector3.Lerp(first, second, t);
        Vector3 c = Vector3.Lerp(second, end, t);
        Vector3 left = Vector3.Lerp(a, b, t);
        Vector3 right = Vector3.Lerp(b, c, t);
        tangent = right - left;
        return Vector3.Lerp(left, right, t);
    }
}
