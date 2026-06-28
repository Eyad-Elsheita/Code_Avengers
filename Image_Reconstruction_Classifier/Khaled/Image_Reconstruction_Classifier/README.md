# Sprint 2 Fixes & Additions

This folder contains fixes for critical bugs found during the code review of the original `Image_Reconstruction_Classifier` project, along with a new logging concept introduced in Sprint 2.

All changes are isolated here until the team agrees to merge them into the main project.

---

## Files

### 1. `ImageProcessing.cs`

**Problem:**
The method `ConvertImagesToBinary` accepted `inputFolderPath` and `outputFolderPath` as parameters but immediately overwrote them with environment variables using the `!` null-forgiving operator. If the environment variables were not set, the application would crash with a null reference exception, and the parameters passed by the caller were completely ignored.

**Fix:**
The method now uses the parameters first. If the parameters are empty or null, it falls back to the environment variables. If neither is set, it throws a clear and descriptive exception instead of crashing silently.

---

### 2. `ImageSpatial.cs`

**Problem:**
Both `SaveImagesinSpartialPooler` and `ProcessTestImagesSpatial` had hardcoded fallback paths pointing to a specific developer's local machine:

```
D:\University\...\Training_Image_Loader
C:\try\...\Training_Image_Spartial
```

These paths would silently fail on any other machine, making the project non-portable and unusable in a cloud or containerized environment.

**Fix:**
All hardcoded paths are removed. The methods now throw a clear `InvalidOperationException` if the required environment variables are not set, making it immediately obvious what is missing instead of failing silently.

---

### 3. `Logger.cs` (New File)

**What it is:**
A new logging concept introduced in Sprint 2 to replace raw `Console.WriteLine` calls throughout the pipeline.

**Methods:**

| Method | Purpose |
|---|---|
| `Logger.LogInfo(message)` | Informational messages about normal flow |
| `Logger.LogError(message)` | Error messages when something goes wrong |
| `Logger.LogDebug(message)` | Detailed messages useful during development |
| `Logger.Log(level, message)` | General log with a custom level label |

**How it works:**
Every log entry is written to both the console and a log file (`app.log`) with a timestamp and level prefix, for example:

```
[2026-06-28 14:32:01] [INFO] Loaded 10000 training images.
[2026-06-28 14:32:05] [ERROR] Environment variable 'Training_Image_Spatial' is not set.
```

The log file location is controlled via the `Log_Output_Path` environment variable. If not set, it defaults to the application's base directory.

---

## Environment Variables Required

For the project to run correctly, the following environment variables must be set:

| Variable | Purpose |
|---|---|
| `Training_Image_Sample` | Input PNG images for training |
| `Training_Image_Binary` | Binarized text files output |
| `Training_Image_Loader` | Vectorized text files |
| `Training_Image_Spatial` | Spatial pooler output |
| `Test_Image_Loader` | Test vectorized files |
| `Test_Image_Spatial` | Test spatial pooler output |
| `Test_Images_Reconstructed_Combined` | Reconstructed PNG output |
| `Similarity_Statistics` | Excel results output folder |
| `Log_Output_Path` | (Optional) Path for the log file |

---

## Status

| File | Status |
|---|---|
| `ImageProcessing.cs` | Fixed — ready for team review |
| `ImageSpatial.cs` | Fixed — ready for team review |
| `Logger.cs` | New — ready for team review |
