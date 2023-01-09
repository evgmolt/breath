namespace TTestApp
{
    internal class DataArrays
    {
        private readonly int _size;
        public double[] RealTimePressureArray;
        public double[] RealTimeTemperatureArray;
        public double[] DCArray;
        public double[] PressureViewArray;
        public double[] TemperatureViewArray;
        public double[] ViewArray;

        public int Size { get { return _size; } }
        public DataArrays(int size)
        {
            _size = size;
            RealTimePressureArray = new double[_size];
            RealTimeTemperatureArray = new double[_size];
            DCArray = new double[_size];
            PressureViewArray = new double[_size];
            TemperatureViewArray = new double[_size];
            ViewArray = new double[_size];
        }


        public String GetDataString(uint index)
        {
            return RealTimePressureArray[index].ToString();
        }
    }
}
