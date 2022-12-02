namespace TTestApp
{
    internal class WaveDetectorEventArgs : EventArgs
    {
        public int WaveCount { get; set; }
        public int Interval { get; set; }
        public int BreathFreq { get; set; }
        public int Index { get; set; }
    }
}
