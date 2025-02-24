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

        // Find the nearest neighbors by comparing the test SDR with training SDRs using Hamming distance
        var nearestNeighbors = trainingData
            .Select(td => new { Label = td.Label, Overlap = Overlap(testSDR, td.SDR) })
            .OrderByDescending(td => td.Overlap)// Sort by ascending distance (nearest first)
            .Take(k) // Take the 'k' nearest neighbors
            .ToList();

        // Use majority vote to determine the class of the test SDR based on the nearest neighbors
        return nearestNeighbors.GroupBy(n => n.Label)
                               .OrderByDescending(g => g.Count()) // Sort by the count of occurrences of each label
                               .First().Key; // Return the label with the highest count
    }

    // Function to calculate the Hamming distance between two SDRs (measuring similarity)
    private static int Overlap(int[] sdr1, int[] sdr2)
    {
        var set1 = new HashSet<int>(sdr1);
        var set2 = new HashSet<int>(sdr2);
        return set1.Intersect(set2).Count();
    }
}
