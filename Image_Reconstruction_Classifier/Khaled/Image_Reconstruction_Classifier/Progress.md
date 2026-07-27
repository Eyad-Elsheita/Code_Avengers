Fix crashes in test image reconstruction pipeline (Khaled copy)

Getting the classifier running locally.

- Update Azure Blob SDK calls with required cancellationToken parameter
  (Tools/ImageLoaderTool.cs, Tools/ImageprocessingTool.cs)
- Fix Khaled/.../Properties/launchSettings.json: replace classmate's local
  paths with mine, and add missing environment variables that were never
  configured at all (Test_Images, Test_Image_Binary, Test_Image_Spatial,
  Test_Images_Reconstructed_Combined) — this was the actual root cause of
  most crashes, since the code silently fell back to empty relative-path
  folders when these were unset
- Fix out-of-bounds array access in HtmClassifier.cs's
  GetPredictedInputValues: imageLength was computed from only the top-1
  scored training example, then used to index into all top-k examples'
  OriginalInput arrays, crashing when a lower-ranked neighbor had a
  shorter array
- Add defensive bounds-check + skip-and-log (via existing Logger.cs) in
  Program.cs's test reconstruction loop, so any remaining data mismatches
  are logged and skipped instead of crashing the whole run
- Add stack trace logging to test image error handler for debugging

Result: full pipeline now runs end-to-end (~18 min), producing valid
similarity statistics (~85-90% avg) in Results/Similarity statistics/