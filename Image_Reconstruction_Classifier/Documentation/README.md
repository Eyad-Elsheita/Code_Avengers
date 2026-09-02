# Image Reconstruction Classifier — Cloud Project

An HTM + KNN image reconstruction pipeline exposed as a set of Model Context Protocol (MCP) tools, backed by Azure Blob and Table Storage, and driven by an AI agent that can talk to either a local or an Azure-hosted instance of the server.

## Architecture

![Deployed architecture diagram](https://github.com/Eyad-Elsheita/Code_Avengers/blob/development/Image_Reconstruction_Classifier/Documentation/Main%20Architeture.jpeg)

An AI agent client calls MCP tools on the containerized MCP server (Azure App Service), which reads/writes Fashion-MNIST images and persisted classifier models to Azure Blob Storage's `train` and `test` containers, and writes per-reconstruction similarity metrics to the `ReconstructionResults` table in Azure Table Storage via `ResultStorageService`.

The repository contains two .NET 9 projects, buildable together via the repo's `.sln` file or independently via `dotnet run --project <name>`:

```
.
├── Image_Reconstruction_Classifier/   # the MCP server ("Main proj")
│   ├── Program.cs
│   ├── Dockerfile
│   ├── Tools/                         # the 8 MCP-callable tools
│   └── ...
└── ImageReconstructionAgent/          # the console AI agent ("Run Agents")
    └── Program.cs
```

## 1. What each project does

**Image_Reconstruction_Classifier** is an ASP.NET Core app that hosts the reconstruction pipeline as an MCP server over HTTP. It exposes eight tools:

| Tool | Responsibility |
|---|---|
| `ImageLoaderTool` | Reads binarized image files from Blob Storage and flattens them into 1-D vectors. |
| `ImageProcessorTool` | Binarizes raw grayscale images at a fixed pixel threshold. |
| `ImageSpatialTool` | Runs the NeoCortexApi spatial pooler to produce each image's SDR. |
| `HtmClassifierTool` | Trains/queries the HTM classifier and produces weighted-average pixel reconstructions. |
| `KnnClassifierTool` | Trains/queries the KNN classifier over SDR overlap, and persists/reloads its model to and from Blob Storage. |
| `ImageSimilarityTool` | Computes cosine and binary similarity between original and reconstructed images. |
| `ImageFilterTool` | Applies a 3×3 median filter to a single binary image to reduce noise (does not itself perform HTM/KNN fusion — see note below). |
| `BinaryToImageConverterTool` | Converts a binary reconstruction back into a viewable PNG. |

Every tool reads its input from and writes its output back to the same Azure Blob container (`train` or `test`) — each call round-trips through a local temp folder internally (download, process, upload, delete) rather than touching a persistent local filesystem path, so there's no local-filesystem *storage* step in this pipeline, only a transient one per call.

**Live vs. offline fusion:** the confidence-weighted HTM/KNN fusion algorithm (confidence-weighted averaging plus Gaussian-weighted local voting) that produces this project's reported accuracy numbers is **not** one of the eight live tools above — it exists only in `LocalPipelineRunner.cs`'s offline batch pipeline. The individual HTM and KNN classifiers are both live and agent-callable; combining their two outputs into one fused reconstruction currently is not. See the paper (Section III-C) for the full disclosure.

Reconstruction results (cosine/binary similarity per image) are additionally written to the `ReconstructionResults` table in Azure Table Storage via `ResultStorageService`.

**ImageReconstructionAgent** is a console app that connects to the MCP server over HTTP, wraps the eight tools as `AITool`s, and hands them to an OpenAI chat model (`gpt-4o-mini` by default) via `Microsoft.Agents.AI`. You type natural-language instructions; the agent decides which tools to call.

## 2. Prerequisites

- .NET 9 SDK
- An Azure Storage account with `train` and `test` Blob containers (Table Storage is created automatically on first run if it doesn't exist)
- An OpenAI API key (only needed to run the agent)
- Docker, if you want to run the server in a container instead of with `dotnet run`

## 3. Environment variables

### Image_Reconstruction_Classifier (the server)

| Variable | Required | Default | Purpose |
|---|---|---|---|
| `AZURE_STORAGE_CONNECTION_STRING` | **Yes** | — (throws `InvalidOperationException` on startup if missing) | Connection string for the storage account backing both the Blob containers and the `ReconstructionResults` table. |
| `ASPNETCORE_URLS` | No | `http://localhost:5280` | Overrides the local listen address. Leave unset locally; Azure App Service sets this itself. |

### ImageReconstructionAgent (the agent)

| Variable | Required | Default | Purpose |
|---|---|---|---|
| `OPENAI_API_KEY` | **Yes** | — (throws `InvalidOperationException` on startup if missing) | Authenticates against the OpenAI API. |
| `OPENAI_CHAT_MODEL_NAME` | No | `gpt-4o-mini` | Which OpenAI chat model drives the agent. |
| `MCP_SERVER_URL` | No | `http://localhost:5280` | Which MCP server to connect to. Point this at your Azure-hosted server's URL to drive the cloud deployment instead of a local one — no code change needed. |

### Azure App Service setting (deployment only, not a local env var)

| Setting | Purpose |
|---|---|
| `WEBSITES_PORT` | Tells App Service which container port to route traffic to (`8080` in our deployment). Set this in the Web App's Configuration blade, not in your shell. |

### Not required to run the product (local-only pipeline)

`LocalPipelineRunner.cs` is a standalone console-style driver that runs the full train/test/evaluate pipeline directly against local folders, entirely outside the MCP server and the agent — it is **not** called from `Program.cs`. It (and the plain `ImageSpatial`/`ImageProcessing` classes it uses) reads a long list of local folder-path environment variables — `Training_Image_Sample`, `Training_Image_Binary`, `Training_Image_Loader`, `Training_Image_Spatial`, `Test_Images`, `Test_Image_Binary`, `Test_Image_Loader`, `Test_Image_Spatial`, `Test_Images_Reconstructed_Binary_HTM`, `Test_Images_Reconstructed_Vector_HTM`, `Test_Images_Reconstructed_Binary_KNN`, `Test_Images_Reconstructed_Vector_KNN`, `Test_Images_Reconstructed_Binary_Combined`, `Test_Images_Reconstructed_Vector_Combined`, `Test_Images_Reconstructed_Combined`, `Similarity_Statistics` — but you only need any of these if you're invoking that class directly for local experimentation. Ignore them if you're just running the server and the agent as described below.

## 4. Running the Main Project (the MCP server)

**With the .NET SDK:**

```bash
export AZURE_STORAGE_CONNECTION_STRING="<your connection string>"
dotnet run --project Image_Reconstruction_Classifier
```

The server starts on `http://localhost:5280` (unless `ASPNETCORE_URLS` overrides it) and exposes the eight tools above over HTTP via `app.MapMcp()`.

**With Docker:**

```bash
docker build -t image-reconstruction-mcp Image_Reconstruction_Classifier
docker run -p 8080:8080 \
  -e AZURE_STORAGE_CONNECTION_STRING="<your connection string>" \
  -e ASPNETCORE_URLS="http://+:8080" \
  image-reconstruction-mcp
```

**Current Azure deployment:** this project runs on Azure App Service for Containers (Linux) as the Web App `image-reconstruction-application` (App Service plan `AnnApiPlan`, Basic B1) in resource group `RG-ImageReconstruction` (Germany West Central), pulling `imgreconstructioncr.azurecr.io/image-reconstruction-mcp:v1` from Azure Container Registry via the App Service's system-assigned Managed Identity (granted the `AcrPull` role) — no registry credentials are stored in the app. Blob/Table Storage is the `imgreconstructionstr` storage account in the same resource group. `WEBSITES_PORT` and `AZURE_STORAGE_CONNECTION_STRING` are set as App Service application settings. To redeploy, build and push a new image tag to that registry and update the Web App's container settings, or check with whoever manages the resource group for the exact release process.

**Verified against the live deployment:** the Azure Portal currently reports this Web App's runtime status as "Issues Detected" — disclosed here rather than omitted. Despite that, pointing `ImageReconstructionAgent` at the live endpoint and asking it a real question works end-to-end (see the captured transcript in Section 5 below), confirming the deployed service actually reads Blob Storage data through the full agent → MCP server → Blob Storage path.

## 5. Running the Agent

Make sure the server (local or Azure-hosted) is already running, then:

```bash
export OPENAI_API_KEY="<your OpenAI API key>"
# optional:
export OPENAI_CHAT_MODEL_NAME="gpt-4o-mini"
export MCP_SERVER_URL="http://localhost:5280"   # or your Azure endpoint

dotnet run --project ImageReconstructionAgent
```

On startup the agent connects to the MCP server, lists the tools it discovered, and drops you into an interactive prompt. Below is an actual captured session against the live Azure-hosted server (`MCP_SERVER_URL` pointed at `https://image-reconstruction-application-gadkh2grhtfug8cp.germanywestcentral-01.azurewebsites.net`), lightly trimmed:

```
Connecting to MCP server at https://image-reconstruction-application-gadkh2grhtfug8cp.germanywestcentral-01.azurewebsites.net
Connected to MCP server

Loaded 25 tools:
  - calculate_cosine_similarity
  - save_reconstructed_image
  - get_image_count
  - apply_median_filter
  - train_classifier
  - process_batch
  - ... (25 total)

=== Image Reconstruction Agent ===
Type 'exit' to quit.
==================================

You: how many images we have in train container
Agent: There are 10,000 images in the train container.
You: exit
```

Type natural-language requests; the agent picks which of the loaded MCP tools to call. Type `exit` (or send a blank line) to end the session.

## 6. Known rough edges

Documented here rather than silently fixed, since these are genuine, verifiable artifacts of the current codebase rather than assumptions:

- The local-only `Training_Image_Spatial` variable's *value* (not the variable name itself, which is spelled correctly) points at a folder literally named `Training_Image_Spartial` in one developer's local path — cosmetic, but worth renaming if that folder is ever recreated elsewhere.
