namespace TTestApp
{
    internal static class DataProcessing
    {
        public static int DerivativeShift = 13;
        public static int DerivativeAverageWidth = 4;

        public static int ValueToMmhG(double value)
        {
            double pressure = value / 19.87;
            return (int)pressure;
        }

        public static double GetDerivative(double[] dataArr, uint Ind)
        {
            if (Ind < DerivativeAverageWidth + DerivativeShift)
            {
                return 0;
            }
            if (Ind - DerivativeAverageWidth + DerivativeAverageWidth > dataArr.Length - 1)
            {
                return 0;
            }
            List<double> L1 = new();
            List<double> L2 = new();
            for (int i = 0; i < DerivativeAverageWidth; i++)
            {
                L1.Add(dataArr[Ind - DerivativeAverageWidth + i]);
                L2.Add(dataArr[Ind - DerivativeAverageWidth - DerivativeShift + i]);
            }
            if (L1.Count > 0 && L2.Count > 0)
            {
                double A1 = L1.Average();
                double A2 = L2.Average();
                return (A1 - A2);
            }
            else
            {
                return 0;
            }
        }

        public static double[] GetSmoothArray(double[] inputArray, int windowSize)
        {
            double[] result = new double[inputArray.Length];
            for (int i = 0; i < result.Length - windowSize; i++)
            {
                double aver = 0;
                for (int j = 0; j < windowSize; j++)
                {
                    aver += inputArray[i + j];
                }
                result[i] = aver /= windowSize;
            }
            return result;
        }
    }
}
