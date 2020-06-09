# FiltfiltSharp
 Zero-phase digital filtering
```
List<double> b = new List<double> { 0.5, 0.5 };
List<double> a = new List<double> { 1 };
List<double> x = new List<double> { 1, 3, 4, 4, 6, 1, 8, 13, 2, 5, 5000 };

List<double> yExpected = new List<double> { 1, 2.75, 3.75, 4.5, 4.25, 4, 7.5, 9, 5.5, 1253, 5000 };

List<double> y = FiltfiltSharp.DoFiltfilt(b, a, x);
```
