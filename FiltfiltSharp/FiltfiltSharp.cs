using System;
using System.Collections.Generic;
using System.Linq;

public class FiltfiltSharp
{
    public static List<double> DoFiltfilt(List<double> b, List<double> a, List<double> x)
	{
        if (b == null) throw new ArgumentNullException(nameof(b));
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (x == null) throw new ArgumentNullException(nameof(x));

        int len = x.Count;
		int na = a.Count;
		int nb = b.Count;
		int nFilt = nb > na ? nb : na;
		int nFact = 3 * (nFilt - 1);

		if (len <= nFact) throw new Exception("Length X is too small");

		Resize(b, nFilt, 0);
		Resize(a, nFilt, 0);

        List<int> rows = new List<int>();
        List<int> cols = new List<int>();

		AddIndexRange(rows, 0, nFilt - 2, 1);
		if (nFilt > 2)
		{
			AddIndexRange(rows, 1, nFilt - 2, 1);
			AddIndexRange(rows, 0, nFilt - 3, 1);
		}

		AddIndexConst(cols, 0, nFilt - 1);
        if (nFilt > 2)
		{
			AddIndexRange(cols, 1, nFilt - 2, 1);
			AddIndexRange(cols, 1, nFilt - 2, 1);
		}
		int kLen = rows.Count;
        List<double> data = new List<double>();
		Resize(data, kLen, 0);
		data[0] = 1 + a[1];
		int j = 1;
		if (nFilt > 2)
		{
			for (int i = 2; i < nFilt; i++)
				data[j++] = a[i];
			for (int i = 0; i < nFilt - 2; i++)
				data[j++] = 1.0;
			for (int i = 0; i < nFilt - 2; i++)
				data[j++] = -1.0;
		}

        List<double> leftPad = SubvectorReverse(x, nFact, 1);
        leftPad = leftPad.Select(q => 2 * x[0] - q).ToList();

        List<double> rightPad = SubvectorReverse(x, len - 2, len - nFact - 1);
        rightPad = rightPad.Select(q => 2 * x[len - 1] - q).ToList();

        List<double> signal1 = new List<double>();
        List<double> signal2 = new List<double>();
        List<double> zi = new List<double>();

        signal1.AddRange(leftPad);
        signal1.AddRange(x);
        signal1.AddRange(rightPad);

        double[][] sp = JaggedArray.CreateJaggedArray<double[][]>(rows.Max() + 1, cols.Max() + 1);

		for (int k = 0; k < kLen; ++k)
			sp[rows[k]][cols[k]] = data[k];

		double[][] zZi = MatrixMultiplication(MatrixInversion(sp), Calc(Segment(b.ToArray(), 1, nFilt - 1), b.ToArray()[0], Segment(a.ToArray(), 1, nFilt - 1)));

		Resize(zi, zZi.Length, 1);

		ChangeZi(zZi, zi, signal1[0]);

		Filter(b, a, signal1, signal2, zi);

        signal2.Reverse();

		ChangeZi(zZi, zi, signal2[0]);

		Filter(b, a, signal2, signal1, zi);

        List<double> y = SubvectorReverse(signal1, signal1.Count - nFact - 1, nFact);
		return y;
    }

    private static void ChangeZi(IReadOnlyList<double[]> zZi, IList<double> zi, double double1)
	{
		for (int i = 0; i < zZi.Count; i++)
            zi[i] = zZi[i][0] * double1;
    }

	private static double[][] Calc(IReadOnlyList<double> segment, double d, IReadOnlyList<double> segment2)
	{
		double[][] ret = JaggedArray.CreateJaggedArray<double[][]>(segment.Count, 1);

		for (int i = 0; i < segment.Count; i++)
            ret[i][0] = segment[i] - d * segment2[i];

		return ret;
	}

	private static double[] Segment(IReadOnlyList<double> bb, int i, int j)
	{
		double[] ret = new double[j - i + 1];

		for (int k = 0; k < j - i + 1; k++)
            ret[k] = bb[i + k];

		return ret;
	}

    public static void Filter(List<double> b, List<double> a, List<double> x, List<double> y, List<double> zi)
	{
		if (a.Count == 0) throw new Exception("Array A is empty");

		bool flagA = true;

        foreach (double doubleA in a)
            if (doubleA != 0)
                flagA = false;

		if (flagA) throw new Exception("An array must have at least one nonzero number");

        if (a[0] == 0) throw new Exception("The first element of array A cannot be zero");

        a = a.Select(q => q / a[0]).ToList();
        b = b.Select(q => q / a[0]).ToList();

        int inputSize = x.Count;
		int filterOrder = Math.Max(a.Count, b.Count);
		Resize(b, filterOrder, 0);
		Resize(a, filterOrder, 0);
		Resize(zi, filterOrder, 0);
		Resize(y, inputSize, 0);

		for (int i = 0; i < inputSize; i++)
		{
			int order = filterOrder - 1;
			while (order != 0)
			{
				if (i >= order)
					zi[order - 1] = b[order] * x[i - order] - a[order] * y[i - order] + zi[order];
				--order;
			}
			y[i] = b[0] * x[i] + zi[0];
		}
		zi.Remove(zi.Count - 1);
	}

	private static void Resize(ICollection<double> a, int i, double j)
	{
		if (a.Count >= i)
			return;
		int size = a.Count;
		for (int j2 = size; j2 < i; j2++)
            a.Add(j);
    }

    private static void AddIndexRange(ICollection<int> indices, int beg, int end, int inc)
	{
		for (int i = beg; i <= end; i += inc)
			indices.Add(i);
	}

    private static void AddIndexConst(ICollection<int> indices, int value, int numel)
	{
		while (numel-- != 0)
			indices.Add(value);
	}

    private static List<double> SubvectorReverse(IReadOnlyList<double> vec, int idxEnd, int idxStart)
	{
        List<double> resultArrayList = new List<double>(idxEnd - idxStart + 1);

		for (int i = 0; i < idxEnd - idxStart + 1; i++)
			resultArrayList.Add(0.0);

		int endIndex = idxEnd - idxStart;

		for (int i = idxStart; i <= idxEnd; i++)
			resultArrayList[endIndex--] = vec[i];

		return resultArrayList;
	}

    public static double[][] MatrixMultiplication(double[][] a, double[][] b)
    {
        int hang = a.Length;
        int lie = b[0].Length;

        double[][] result = JaggedArray.CreateJaggedArray<double[][]>(hang, lie);

        for (int i = 0; i < hang; i++)
        for (int j = 0; j < lie; j++)
            result[i][j] = b.Select((t, k) => a[i][k] * t[j]).Sum();

        return result;
    }

    public static double[][] MatrixInversion(double[][] matrix)
    {
        int matrixLength = matrix.Length;

        double[][] matrixTemp = JaggedArray.CreateJaggedArray<double[][]>(matrixLength, 2 * matrixLength);

        double[][] result = JaggedArray.CreateJaggedArray<double[][]>(matrixLength, matrixLength);

        for (int i = 0; i < matrixLength; i++)
        for (int j = 0; j < matrixLength; j++)
            matrixTemp[i][j] = matrix[i][j];

        for (int k = 0; k < matrixLength; k++)
        for (int t = matrixLength; t < matrixLength * 2; t++)
            matrixTemp[k][t] = t - k == matrixLength ? 1.0 : 0;

        for (int k = 0; k < matrixLength; k++)
        {
            if (matrixTemp[k][k] != 1)
            {
                double bs = matrixTemp[k][k];
                matrixTemp[k][k] = 1;
                for (int p = k; p < matrixLength * 2; p++)
                    matrixTemp[k][p] /= bs;
            }
            for (int q = 0; q < matrixLength; q++)
            {
                if (q == k) 
                    continue;

                double bs = matrixTemp[q][k];

                for (int p = 0; p < matrixLength * 2; p++)
                    matrixTemp[q][p] -= bs * matrixTemp[k][p];
            }
        }
        for (int x = 0; x < matrixLength; x++)
        for (int y = matrixLength; y < matrixLength * 2; y++)
            result[x][y - matrixLength] = matrixTemp[x][y];

        return result;
    }
}
