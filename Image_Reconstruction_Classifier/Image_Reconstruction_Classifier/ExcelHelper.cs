using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

public class ExcelHelper
{
    public static void SaveSimilarityStatistics(List<SimilarityData> similarityDataList, string filePath)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Similarity Statistics");

        // Add headers to the sheet
        worksheet.Cell(1, 1).Value = "Picture Name";
        worksheet.Cell(1, 2).Value = "Vector Similarity Percentage";
        worksheet.Cell(1, 3).Value = "Binary Similarity Percentage";
        worksheet.Cell(1, 4).Value = "Average Vector Similarity Percentage";
        worksheet.Cell(1, 5).Value = "Average Binary Similarity Percentage";

        // Add data rows
        for (int i = 0; i < similarityDataList.Count; i++)
        {
            var data = similarityDataList[i];
            worksheet.Cell(i + 2, 1).Value = data.PictureName;
            worksheet.Cell(i + 2, 2).Value = Math.Round(data.VectorSimilarityPercentage, 2);
            worksheet.Cell(i + 2, 3).Value = Math.Round(data.BinarySimilarityPercentage, 2);
        }

        // Calculate and add the averages
        double avgVectorSimilarity = similarityDataList.Average(d => d.VectorSimilarityPercentage);
        double avgBinarySimilarity = similarityDataList.Average(d => d.BinarySimilarityPercentage);

        worksheet.Cell(2, 4).Value = Math.Round(avgVectorSimilarity, 2);
        worksheet.Cell(2, 5).Value = Math.Round(avgBinarySimilarity, 2);

        // Save the workbook
        workbook.SaveAs(filePath);
    }
}

public class SimilarityData
{
    public string PictureName { get; set; } = string.Empty;  // Default value to avoid nullability issues.
    public double VectorSimilarityPercentage { get; set; }
    public double BinarySimilarityPercentage { get; set; }
}
