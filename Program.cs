using System;
using System.IO;
using System.Security;
using OfficeOpenXml;

class Program
{
    static void Main(string[] args)
    {
        string filePath = "C:/Users/padel/Desktop/algorai/InzinerinisProj/IP_places_data_2025.xlsx";
        var places = IO.ReadFile(filePath);
        var matrix = IO.BuildMatrix(places);

        IO.PrintMatrix(matrix); // 5x5

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
    public static double[,] BuildMatrix(List<Place> places)
    {
        int n = places.Count;
        double[,] distanceMatrix = new double[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (i == j)
                {
                    distanceMatrix[i, j] = 0.0;
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
}
public class Place
{
    public int Number { get; set; }
    public string Name { get; set; }
    public long Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}
