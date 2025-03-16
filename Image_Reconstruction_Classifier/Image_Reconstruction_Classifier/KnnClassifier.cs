using System;
using System.Collections.Generic;
using System.Linq;

public class KnnClassifier
{
    private readonly List<(int[] SDR, int Label)> trainingData = new();

    // Function to add training data to the KNN model
    public void Train(int[] sdr, int label)
    {
        trainingData.Add((sdr, label));
    }

    // Function to classify a new SDR using KNN algorithm
    public int Classify(int[] testSDR, int k)
    {
        if (trainingData.Count == 0)
            throw new InvalidOperationException("No training data available!");

        // Find the nearest neighbors by comparing the test SDR with training SDRs using overlap (similarity)
        var nearestNeighbors = trainingData
            .Select(td => new { Label = td.Label, Overlap = Overlap(testSDR, td.SDR) })
            .OrderByDescending(td => td.Overlap) // Higher overlap = more similar
            .Take(k) // Take the 'k' nearest neighbors
            .ToList();

        // Use weighted voting with squared overlap weights
        var labelScores = new Dictionary<int, double>();
        foreach (var neighbor in nearestNeighbors)
        {
            if (!labelScores.ContainsKey(neighbor.Label))
                labelScores[neighbor.Label] = 0;

            // Use squared overlap to emphasize stronger similarities
            labelScores[neighbor.Label] += Math.Pow(neighbor.Overlap, 2);
        }

        // Return the label with the highest weighted score
        return labelScores.OrderByDescending(kvp => kvp.Value).First().Key;
    }

    // Function to calculate the Hamming-style overlap between two SDRs (number of shared active bits)
    private static int Overlap(int[] sdr1, int[] sdr2)
    {
        var set1 = new HashSet<int>(sdr1);
        var set2 = new HashSet<int>(sdr2);
        return set1.Intersect(set2).Count();
    }
}