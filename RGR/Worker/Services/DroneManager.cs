using static SharedLibrary.DataModel;
using System.Numerics;
using System.Text;

namespace Orchestrator.Services;

public class DroneManager
{
    static readonly float boidSpeed = 10f;

    static readonly float forceDistance = 20.0f;
    static readonly float separationFactor = 10f;
    static readonly float cohesionFactor = 1f;
    static readonly float alignmentFactor = 2f;
    static readonly float altitudeFactor = 1f;
    static readonly float targetFactor = 10f;

    public static List<(int, SerializableVector)> UpdateDrones
    (
        Dictionary<int, DroneData> Drones,
        float altitude, 
        Vector3 target, 
        (int, int)? split = null
    )
    {
        List<(int, SerializableVector)> result = [];
        float separationFactorSqr = separationFactor * separationFactor;
        float forceDistanceSqr = forceDistance * forceDistance;
      
        var splitRange = split == null ? (0, Drones.Count-1) : (split.Value.Item1, split.Value.Item2);
        var (s, e) = splitRange;
        var splitKeys = Drones.Keys.Skip(s).Take(e + 1 - s);

        foreach (var k in splitKeys)
        {
            var d = Drones[k];
            Vector3 sumForce = Vector3.Zero;
            
            int visibleCount = 0;

            Vector3 positionSum = Vector3.Zero;
            Vector3 directionSum = Vector3.Zero;
            Vector3 repulsionSum = Vector3.Zero;

            var pos = d.position.ToVector3();
            var vel = d.velocity.ToVector3();

            foreach (var (other_k, other_d) in Drones)
            {
                if (other_k == k) continue;

                var otherPos = other_d.position.ToVector3();

                var diff = otherPos - pos;
                var dstSquared = diff.LengthSquared();

                if (dstSquared < forceDistanceSqr)
                {
                    var dir = Vector3.Normalize(diff);
                    repulsionSum += (1 / dstSquared) * -dir;

                    positionSum += otherPos;
                    visibleCount++;

                    directionSum += other_d.velocity.ToVector3();
                }
            }

            if (visibleCount != 0)
            {
                // Separation
                sumForce += repulsionSum * separationFactorSqr;

                // Cohesion
                sumForce += (positionSum / visibleCount - pos) * cohesionFactor;

                // Alignment
                var alignmentForce = NormalizeNoNaN(directionSum / visibleCount) * alignmentFactor;
                sumForce += ProjectOnPlane(alignmentForce, vel);
            }

            // Target
            Vector3 tgtDiff = target - pos;
            float dist = tgtDiff.Length();
            Vector3 tgtDir = NormalizeNoNaN(tgtDiff);
            float targetForce = Math.Clamp(dist/20, 0, 1);
            sumForce += tgtDir * targetForce * targetFactor;

            var newVelocity = NormalizeSpeed(vel + sumForce * (1/30f));

            result.Add((k, SerializableVector.FromVector3(newVelocity)));
        }
        return result;
    }

    public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
    {
        planeNormal = NormalizeNoNaN(planeNormal);

        float dot = Vector3.Dot(vector, planeNormal);

        return vector - (dot * planeNormal);
    }

    private static Vector3 NormalizeNoNaN(Vector3 v)
    {
        var l = v.Length();
        return l == 0 ? v : v / l;
    }

    public static Vector3 NormalizeSpeed(Vector3 velocity)
    {
        var changedSpeed = velocity.Length();
        if (changedSpeed == 0) return Vector3.Zero;
        return NormalizeNoNaN(velocity) * Math.Clamp(changedSpeed, boidSpeed / 2, boidSpeed);
    }
}
