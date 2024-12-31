using System.Numerics;

namespace SharedLibrary;

public class DataModel
{
    [Serializable]
    public record class SerializableVector
    {
        public required float x { get; set; }
        public required float y { get; set; }
        public required float z { get; set; }
        
        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
        public static SerializableVector FromVector3(Vector3 v)
        {
            return new SerializableVector() { x = v.X, y = v.Y, z = v.Z };
        }
    }

    [Serializable]
    public record class DroneData
    {
        public required SerializableVector position { get; set; }
        public required SerializableVector velocity { get; set; }
    }

}
