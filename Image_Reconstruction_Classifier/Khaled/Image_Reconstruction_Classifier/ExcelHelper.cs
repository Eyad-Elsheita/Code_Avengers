using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Helps save similarity statistics to an Excel file.
/// </summary>
public class ExcelHelper
{
    /// <summary>
    /// Saves similarity statistics to an Excel file at the specified file path.
    /// </summary>
    /// <param name="similarityDataList">List of similarity data objects to be written to the Excel file.</param>
    /// <param name="filePath">The file path where the Excel file will be saved.</param>
    public static void SaveSimilarityStatistics(List<SimilarityData> similarityDataList, string filePath)
    {
        // Create a new Excel workbook using ClosedXML.
        var workbook = new XLWorkbook();
        // Add a worksheet named "Similarity Statistics".
        var worksheet = workbook.AddWorksheet("Similarity Statistics");

        // Add headers to the sheet. These headers label the columns for the data.
        worksheet.Cell(1, 1).Value = "Picture Name";
        worksheet.Cell(1, 2).Value = "Vector Similarity Percentage";
        worksheet.Cell(1, 3).Value = "Binary Similarity Percentage";
        worksheet.Cell(1, 4).Value = "Average Vector Similarity Percentage";
        worksheet.Cell(1, 5).Value = "Average Binary Similarity Percentage";
        worksheet.Cell(1, 6).Value = "StdDev Vector Similarity Percentage";
        worksheet.Cell(1, 7).Value = "StdDev Binary Similarity Percentage";

        // Loop through the list of similarity data and add each item as a new row in the worksheet.
        for (int i = 0; i < similarityDataList.Count; i++)
        {
            var data = similarityDataList[i];
            // Set the picture name.
            worksheet.Cell(i + 2, 1).Value = data.PictureName;
            // Set the vector similarity percentage rounded to 2 decimal places.
            worksheet.Cell(i + 2, 2).Value = Math.Round(data.VectorSimilarityPercentage, 2);
            // Set the binary similarity percentage rounded to 2 decimal places.
            worksheet.Cell(i + 2, 3).Value = Math.Round(data.BinarySimilarityPercentage, 2);
        }

        // Calculate averages for both vector and binary similarity percentages.
        double avgVectorSimilarity = similarityDataList.Average(d => d.VectorSimilarityPercentage);
        double avgBinarySimilarity = similarityDataList.Average(d => d.BinarySimilarityPercentage);

        // Calculate standard deviations for both vector and binary similarity percentages.
        double stdevVectorSimilarity = CalculateStandardDeviation(similarityDataList.Select(d => d.VectorSimilarityPercentage));
        double stdevBinarySimilarity = CalculateStandardDeviation(similarityDataList.Select(d => d.BinarySimilarityPercentage));

        // Add the calculated averages and standard deviations to the worksheet.
        // The averages and standard deviations are written into row 2, columns 4, 5, 6, and 7.
        worksheet.Cell(2, 4).Value = Math.Round(avgVectorSimilarity, 2);
        worksheet.Cell(2, 5).Value = Math.Round(avgBinarySimilarity, 2);
        worksheet.Cell(2, 6).Value = Math.Round(stdevVectorSimilarity, 2);
        worksheet.Cell(2, 7).Value = Math.Round(stdevBinarySimilarity, 2);

        // Save the workbook to the specified file path.
        workbook.SaveAs(filePath);
    }

    /// <summary>
    /// Calculates the sample standard deviation of a collection of values.
    /// </summary>
    /// <param name="values">The collection of values to calculate the standard deviation for.</param>
    /// <returns>The sample standard deviation.</returns>
    private static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var valueList = values.ToList();
        if (valueList.Count <= 1)
            return 0;

        double average = valueList.Average();
        double sumOfSquares = valueList.Sum(v => (v - average) * (v - average));
        return Math.Sqrt(sumOfSquares / (valueList.Count - 1));
    }
}

/// <summary>
/// Represents similarity data for a picture.
/// </summary>
public class SimilarityData
{
    // Holds the picture name. Default value is an empty string to avoid nullability issues.
    public string PictureName { get; set; } = string.Empty;
    // Holds the vector similarity percentage.
    public double VectorSimilarityPercentage { get; set; }
    // Holds the binary similarity percentage.
    public double BinarySimilarityPercentage { get; set; }
}