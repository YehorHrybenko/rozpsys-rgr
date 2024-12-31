
using static SharedLibrary.DataModel;

namespace SharedLibrary;

public class CalculationsModel {
    public record UpdateRequest
    {
        public int droneCount { get; set; }
        public string data { get; set; } = "";

        public float altitude { get; set; } = 20f;
        public string target { get; set; } = "";
    }

    public record CalculationsRequest
    {
        public int requestID;
        public string data { get; set; } = "";

        public float altitude { get; set; } = 20f;
        public string target { get; set; } = "";

        public (int start, int end) slice { get; set; }

        public string replyTo = "";

        public void Deconstruct(out int requestID, out string data, out (int start, int end) slice, out string replyTo, out float altitude, out string target)
        {
            requestID = this.requestID;
            data = this.data;
            slice = this.slice;
            replyTo = this.replyTo;
            altitude = this.altitude;
            target = this.target;
        }
    }


    public record CalculationsResponse
    {
        public int requestID;
        public string result = "";

        public void Deconstruct(out int requestID, out string result)
        {
            requestID = this.requestID;
            result = this.result;
        }
    }

    public record StorageRequest
    {
        public int droneID;
        public string droneData = "";
    }

}
