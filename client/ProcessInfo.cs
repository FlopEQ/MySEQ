namespace myseq
{
    public class ProcessInfo
    {
        public ProcessInfo(int ProcessID, string sCharName, string zoneName = "", bool isCurrent = false)
        {
            this.ProcessID = ProcessID;
            SCharName = sCharName;
            ZoneName = zoneName;
            IsCurrent = isCurrent;
        }

        public int ProcessID { get; set; }
        public string SCharName { get; set; }
        public string ZoneName { get; set; }
        public bool IsCurrent { get; set; }

        public string DisplayName
        {
            get
            {
                var character = string.IsNullOrWhiteSpace(SCharName) ? "Unknown character" : SCharName;
                var zone = string.IsNullOrWhiteSpace(ZoneName) ? "unknown zone" : ZoneName;
                return $"{character} - {zone} (PID {ProcessID})";
            }
        }
    }
}
