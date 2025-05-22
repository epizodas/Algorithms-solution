using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using OfficeOpenXml;
using ScottPlot;
using ScottPlot.Plottables;

class Program
{
    private const double INF = double.MaxValue;
    static void Main(string[] args)
    {
        string filePath = "C:/Users/padel/Desktop/algorai/InzinerinisProj/IP_places_data_2025.xlsx";
        var places = IO.ReadFile(filePath);
        var matrixFull = IO.BuildMatrix(places, places.Count);

        int startIndex = places.FindIndex(p => p.Number == 5);
        var (bus1, bus2) = Solver.Greedy(matrixFull, startIndex);
        //IO.PrintRoute(bus1, places, "Autobusiukas 1");
        //IO.PrintRoute(bus2, places, "Autobusiukas 2");
        //IO.PlotRoutes(bus1, bus2, places, "marsrutai.png");

        var matrix = IO.BuildMatrix(places, 14);
        Solver.BranchBound(matrix, startIndex, 10);
    }
    public static class Solver
    {
        // 1 dalis
        public static (Route, Route) Greedy(double[,] matrix, int startIndex)
        {
            int n = matrix.GetLength(0);
            bool[] visited = new bool[n];
            visited[startIndex] = true;

            var route1 = new Route();
            var route2 = new Route();

            route1.Path.Add(startIndex);
            route2.Path.Add(startIndex);

            int remaining = n - 1;

            int pos1 = startIndex;
            int pos2 = startIndex;

            while (remaining > 0)
            {
                // Autobusiukas 1 renkasi artimiausią
                int next1 = FindNearest(matrix, pos1, visited);
                if (next1 != -1)
                {
                    route1.Distance += matrix[pos1, next1];
                    pos1 = next1;
                    visited[pos1] = true;
                    route1.Path.Add(pos1);
                    remaining--;
                }

                if (remaining == 0) break;

                // Autobusiukas 2 renkasi artimiausią
                int next2 = FindNearest(matrix, pos2, visited);
                if (next2 != -1)
                {
                    route2.Distance += matrix[pos2, next2];
                    pos2 = next2;
                    visited[pos2] = true;
                    route2.Path.Add(pos2);
                    remaining--;
                }
            }

            // Grįžtam į pradžią
            route1.Distance += matrix[pos1, startIndex];
            route2.Distance += matrix[pos2, startIndex];
            route1.Path.Add(startIndex);
            route2.Path.Add(startIndex);

            return (route1, route2);
        }
        private static int FindNearest(double[,] matrix, int from, bool[] visited)
        {
            int n = matrix.GetLength(0);
            double minDist = double.MaxValue;
            int nearest = -1;

            for (int i = 0; i < n; i++)
            {
                if (!visited[i] && matrix[from, i] < minDist)
                {
                    minDist = matrix[from, i];
                    nearest = i;
                }
            }

            return nearest;
        }

        // 2 dalis
        public class Node : IComparable<Node>
        {
            public double[,] reducedMatrix;
            public List<int> Path;
            public double Cost;
            public int Vertex;
            public int Level;

            public Node(double[,] parentMatrix, List<int> path, int level, int i, int j)
            {
                Path = new List<int>(path);
                if (level != 0)
                    Path.Add(j);

                reducedMatrix = new double[parentMatrix.GetLength(0), parentMatrix.GetLength(1)];
                Array.Copy(parentMatrix, reducedMatrix, parentMatrix.Length);

                for (int k = 0; level != 0 && k < reducedMatrix.GetLength(0); k++)
                {
                    reducedMatrix[i, k] = INF;
                    reducedMatrix[k, j] = INF;
                }

                if (level != 0)
                    reducedMatrix[j, 0] = INF;

                Cost = ReduceMatrix(reducedMatrix);
                Vertex = j;
                Level = level;
            }
            public int CompareTo(Node other)
            {
                return Cost.CompareTo(other.Cost);
            }
        }
        public static double ReduceMatrix(double[,] matrix)
        {
            double cost = 0;
            int n = matrix.GetLength(0);
            // Row reduction
            for (int i = 0; i < n; i++)
            {
                double rowMin = INF;
                for (int j = 0; j < n; j++)
                    if (matrix[i, j] < rowMin)
                        rowMin = matrix[i, j];

                if (rowMin != INF && rowMin != 0)
                {
                    for (int j = 0; j < n; j++)
                        if (matrix[i, j] != INF)
                            matrix[i, j] -= rowMin;
                    cost += rowMin;
                }
            }
            // Column reduction
            for (int j = 0; j < n; j++)
            {
                double colMin = INF;
                for (int i = 0; i < n; i++)
                    if (matrix[i, j] < colMin)
                        colMin = matrix[i, j];

                if (colMin != INF && colMin != 0)
                {
                    for (int i = 0; i < n; i++)
                        if (matrix[i, j] != INF)
                            matrix[i, j] -= colMin;
                    cost += colMin;
                }
            }
            return cost;
        }
        public static double[,] BuildSubMatrix(double[,] matrix, List<int> indices)
        {
            int n = indices.Count;
            var sub = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    sub[i, j] = matrix[indices[i], indices[j]];
            return sub;
        }
        public static void BranchBound(double[,] matrix, int startIndex, int timeLimitSec = 10)
        {
            int n = matrix.GetLength(0);
            var allPlaces = Enumerable.Range(0, n).Where(i => i != startIndex).ToArray();
            double bestMax = INF;
            List<int> best1 = null, best2 = null;
            List<int> bestPath1 = null, bestPath2 = null;
            double bestCost1 = 0, bestCost2 = 0;

            var sw = Stopwatch.StartNew();
            int maxMask = 1 << allPlaces.Length;
            for (int mask = 1; mask < maxMask / 2; mask++)
            {
                if (sw.Elapsed.TotalSeconds > timeLimitSec)
                    break;

                var bus1 = new List<int> { startIndex };
                var bus2 = new List<int> { startIndex };
                for (int i = 0; i < allPlaces.Length; i++)
                {
                    if ((mask & (1 << i)) != 0)
                        bus1.Add(allPlaces[i]);
                    else
                        bus2.Add(allPlaces[i]);
                }

                if (bus1.Count < 2 || bus2.Count < 2)
                    continue;

                var subMatrix1 = BuildSubMatrix(matrix, bus1);
                var subMatrix2 = BuildSubMatrix(matrix, bus2);

                var (path1, cost1) = SolveTSP(subMatrix1, 0);
                var (path2, cost2) = SolveTSP(subMatrix2, 0);

                double maxCost = Math.Max(cost1, cost2);
                if (maxCost < bestMax)
                {
                    bestMax = maxCost;
                    best1 = new List<int>(bus1);
                    best2 = new List<int>(bus2);
                    bestPath1 = path1.Select(idx => bus1[idx]).ToList();
                    bestPath2 = path2.Select(idx => bus2[idx]).ToList();
                    bestCost1 = cost1;
                    bestCost2 = cost2;
                }
            }

            Console.WriteLine("\nOptimalūs maršrutai (Branch and Bound):");
            Console.WriteLine($"Autobusiukas 1 ({bestCost1:F2}): {string.Join(" -> ", bestPath1.Select(i => i + 1))}");
            Console.WriteLine($"Autobusiukas 2 ({bestCost2:F2}): {string.Join(" -> ", bestPath2.Select(i => i + 1))}");
            Console.WriteLine($"Minimalus maksimalus ilgis: {bestMax:F2}");
        }

        public static (List<int> path, double cost) SolveTSP(double[,] matrix, int start)
        {
            int n = matrix.GetLength(0);
            var pq = new PriorityQueue<Node, double>();
            var path = new List<int> { start };
            var root = new Node(matrix, path, 0, -1, start);
            pq.Enqueue(root, root.Cost);

            double minCost = INF;
            List<int> bestPath = null;

            while (pq.Count > 0)
            {
                Node min = pq.Dequeue();

                if (min.Level == n - 1)
                {
                    min.Path.Add(start);
                    double totalCost = min.Cost + matrix[min.Vertex, start];
                    if (totalCost < minCost)
                    {
                        minCost = totalCost;
                        bestPath = new List<int>(min.Path);
                    }
                    continue;
                }

                for (int j = 0; j < n; j++)
                {
                    if (min.reducedMatrix[min.Vertex, j] != INF)
                    {
                        double travelCost = matrix[min.Vertex, j];
                        Node child = new Node(min.reducedMatrix, min.Path, min.Level + 1, min.Vertex, j);
                        child.Cost += min.Cost + travelCost;

                        if (child.Cost < minCost)
                            pq.Enqueue(child, child.Cost);
                    }
                }
            }

            return (bestPath, minCost);
        }
    }
public class IO
    {
        public static List<Place> ReadFile(string filePath)
        {
            var places = new List<Place>();
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return places;
            }
            ExcelPackage.License.SetNonCommercialPersonal("eripad");
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int startRow = 5;
                int endRow = 318;
                int startCol = 2;
                int endCol = 6;

                for (int row = 5; row <= 318; row++)
                {
                    var place = new Place
                    {
                        Number = int.Parse(worksheet.Cells[row, 2].Text),
                        Name = worksheet.Cells[row, 3].Text,
                        Id = long.Parse(worksheet.Cells[row, 4].Text),
                        X = double.Parse(worksheet.Cells[row, 5].Text),
                        Y = double.Parse(worksheet.Cells[row, 6].Text)
                    };
                    places.Add(place);
                }
            }
            return places;
        }
        public static double[,] BuildMatrix(List<Place> places, int n)
        {
            double[,] distanceMatrix = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        distanceMatrix[i, j] = INF;
                    }
                    else
                    {
                        double dx = places[j].X - places[i].X;
                        double dy = places[j].Y - places[i].Y;
                        distanceMatrix[i, j] = Math.Sqrt(dx * dx + dy * dy);
                    }
                }
            }
            return distanceMatrix;
        }

        public static void PrintPlaces(List<Place> places)
        {
            foreach (var place in places)
            {
                Console.WriteLine($"{place.Number} | {place.Name} | {place.Id} | {place.X} | {place.Y}");
            }
        }
        public static void PrintMatrix(double[,] matrix)
        {
            Console.WriteLine("\nDistance Matrix Sample [0-4][0-4]:");
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Console.Write($"{matrix[i, j]:F2}\t");
                }
                Console.WriteLine();
            }
        }
        public static void PrintRoute(Route route, List<Place> places, string label)
        {
            Console.WriteLine($"\n{label} (Total Distance: {route.Distance:F2})");
            foreach (var index in route.Path)
            {
                Console.Write($"{places[index].Number}.{places[index].Name} -> ");
            }
            Console.WriteLine("END");
        }
        public static void PlotRoutes(Route route1, Route route2, List<Place> places, string filePath)
        {
            var plt = new Plot();

            double[] x1 = route1.Path.Select(i => places[i].X).ToArray();
            double[] y1 = route1.Path.Select(i => places[i].Y).ToArray();

            double[] x2 = route2.Path.Select(i => places[i].X).ToArray();
            double[] y2 = route2.Path.Select(i => places[i].Y).ToArray();

            var scatter1 = plt.Add.Scatter(x1, y1, Color.FromColor(System.Drawing.Color.Red));
            scatter1.LegendText = "Bus 1";
            var scatter2 = plt.Add.Scatter(x2, y2, Color.FromColor(System.Drawing.Color.Blue));
            scatter2.LegendText = "Bus 2";

            plt.Title("Maršrutų vizualizacija");
            plt.XLabel("X");
            plt.YLabel("Y");

            plt.SavePng(filePath, 800, 600);
            Console.WriteLine($"Grafikas išsaugotas: {filePath}");
        }
    }
    public class Place
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public long Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
    public class Route
    {
        public List<int> Path { get; set; } = new List<int>();
        public double Distance { get; set; } = 0.0;
    }
}