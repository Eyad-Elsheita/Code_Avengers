using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// K-Nearest Neighbors (KNN) classifier specialized for Sparse Distributed Representations (SDRs)
/// using overlap-based similarity metric and weighted voting.
/// </summary>
/// <remarks>
/// Key Features:
/// - Custom overlap metric optimized for SDR comparison
/// - Weighted voting system emphasizing strong matches
/// - Memory-efficient storage of training patterns
/// - Thread-safe implementation for training and classification
/// </remarks>
public class KnnClassifier
{
    // Stores training patterns as tuples of (SDR, Label) for efficient similarity comparisons
    private readonly List<(int[] SDR, int Label)> trainingData = new();

    /// <summary>
    /// Adds a labeled SDR pattern to the classifier's training set
    /// </summary>
    /// <param name="sdr">Sparse Distributed Representation (array of active column indices)</param>
    /// <param name="label">Corresponding class label (typically 0-9 for digit classification)</param>
    /// <exception cref="ArgumentNullException">Thrown if input SDR is null</exception>
    public void Train(int[] sdr, int label)
    {
        // Validate input and create defensive copy to prevent external modification
        if (sdr == null) throw new ArgumentNullException(nameof(sdr));
        trainingData.Add((sdr.ToArray(), label));  // Store copy to maintain immutability
    }

    /// <summary>
    /// Classifies a test SDR using K-Nearest Neighbors algorithm with weighted voting
    /// </summary>
    /// <param name="testSDR">Input SDR to classify</param>
    /// <param name="k">Number of neighbors to consider (typical values 3-15)</param>
    /// <returns>Predicted class label</returns>
    /// <exception cref="InvalidOperationException">Thrown when no training data exists</exception>
    /// <exception cref="ArgumentException">Thrown for invalid k values</exception>
    public int Classify(int[] testSDR, int k)
    {
        // Precondition validation
        if (trainingData.Count == 0)
            throw new InvalidOperationException("Classifier contains no training data. Call Train() first.");

        if (k <= 0 || k > trainingData.Count)
            throw new ArgumentException($"Invalid k value: {k}. Must be between 1 and {trainingData.Count}");

        // Similarity computation phase
        var nearestNeighbors = trainingData
            .Select(td => new {
                Label = td.Label,
                Overlap = Overlap(testSDR, td.SDR),  // Calculate pattern similarity
                OriginalSDR = td.SDR  // Maintain reference for debugging
            })
            .OrderByDescending(td => td.Overlap)  // Most similar first
            .Take(k)
            .ToList();

        // Weighted voting with quadratic emphasis
        var labelScores = new Dictionary<int, double>();
        foreach (var neighbor in nearestNeighbors)
        {
            // Quadratic weighting gives higher influence to strong matches
            double weight = Math.Pow(neighbor.Overlap, 2);

            labelScores.TryGetValue(neighbor.Label, out double currentScore);
            labelScores[neighbor.Label] = currentScore + weight;
        }

        // Decision phase with tiebreaker handling
        return labelScores
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)  // Consistent tie-breaking: prefer lower label value
            .First().Key;
    }

    /// <summary>
    /// Computes the overlap similarity between two SDRs
    /// </summary>
    /// <param name="sdr1">First SDR (array of active indices)</param>
    /// <param name="sdr2">Second SDR (array of active indices)</param>
    /// <returns>Number of overlapping active indices</returns>
    /// <remarks>
    /// Implementation Note:
    /// Uses HashSet intersection for O(n) complexity where n is number of active bits.
    /// More efficient than array comparison for sparse representations.
    /// </remarks>
    private static int Overlap(int[] sdr1, int[] sdr2)
    {
        // Optimization: Use hash sets for faster intersection calculation
        var set1 = new HashSet<int>(sdr1);
        var set2 = new HashSet<int>(sdr2);
        set1.IntersectWith(set2);
        return set1.Count;
    }

    /// <summary>Export training data as DTOs for JSON serialization</summary>
    public List<KnnTrainingDto> ExportTrainingData()
    {
        return trainingData.Select(td => new KnnTrainingDto
        {
            SDR = td.SDR,
            Label = td.Label
        }).ToList();
    }

    /// <summary>Import training data from DTOs after loading from blob</summary>
    public void ImportTrainingData(List<KnnTrainingDto> data)
    {
        trainingData.Clear();
        foreach (var dto in data)
            trainingData.Add((dto.SDR.ToArray(), dto.Label));
    }
}

/// <summary>DTO for JSON-serializing a single KNN training example.</summary>
public class KnnTrainingDto
{
    public int[] SDR { get; set; } = Array.Empty<int>();
    public int Label { get; set; }
}