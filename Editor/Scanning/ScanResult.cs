using Fiascko.ReverseFiascko.Editor.Core;

namespace Fiascko.ReverseFiascko.Editor.Scanning
{
    public class ScanResult
    {
        public string Name { get; }
        public string Details { get; }
        public ScanStatus Status { get; }

        public ScanResult(string name, string details, ScanStatus status)
        {
            Name = name;
            Details = details;
            Status = status;
        }
    }
}
