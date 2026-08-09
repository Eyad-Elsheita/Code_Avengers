# Sprint Plan - Image Reconstruction Classifier on Azure

**Deadline: August 23, 2026 | Open review issue by: August 16**

---

## Sprint 1 (June 1–14)

**Goal: Setup & Foundation**

- Clone the template from the repo
- Set up Azure Storage account with two blob containers: `train` and `test`
- Set up environment variables for the storage connection string
- Add `SampleFiles` folder with test images committed
- Write skeleton MCP tool classes for image loading and processing

**Deliverable:** Repo structure visible, first commits pushed

---

## Sprint 2 (June 15–28)

**Goal: Core Experiment Pipeline Wired to Blob Storage**

- Implement `ImageLoaderTool` — load, list, and count images from `train`/`test` containers
- Implement `ImageProcessorTool` — download PNGs, binarize via `ImageBinarizer`, upload `*_binarized.txt` back to the same container
- Implement `ImageSpatialTool` — run the HTM Spatial Pooler (train + inference modes) on encoded vectors, upload `*_spatial.txt`
- Implement `HtmClassifierTool` — train per-object-type HTM classifiers (kept in an in-memory dictionary), reconstruct images from spatial SDRs
- Implement `BinaryToImageConverterTool` — convert reconstructed binary arrays back to PNG and upload
- Implement `ImageFilterTool` and `ImageSimilarityTool` — median filtering and cosine/binary similarity scoring
- Each stage chains to the next via filename convention (e.g. `3_001.png` → `3_001_binarized.txt` → `3_001_spatial.txt` → `3_001_htm_reconstructed.txt`)

**Deliverable:** Full pipeline runs end-to-end locally — load → binarize → spatial pool → train/reconstruct → filter → score — all via direct method calls against blob storage


---

## Sprint 3 (June 29 – July 12)

**Goal: MCP Tool Integration**

- Expose all seven pipeline stages as MCP Tools (`[McpServerTool]` methods across `ImageLoaderTool`, `ImageProcessorTool`, `ImageSpatialTool`, `HtmClassifierTool`, `BinaryToImageConverterTool`, `ImageFilterTool`, `ImageSimilarityTool`)
- Support both STDIO and HTTP transport
- Register all tools in the MCP server
- Test an MCP client triggering individual pipeline stages directly (e.g. calling `TrainClassifier` or `ReconstructImage` by name, with explicit parameters)
- Write `Experiment Specification - Firstname Lastname.md`

**Deliverable:** MCP server runs and an MCP client can trigger any stage of the pipeline via a direct tool call — no queue or polling involved


---

## Sprint 4 (July 13–26)

**Goal: Dockerize**

- Write `Dockerfile` for the MCP server (multi-stage build: `dotnet/sdk:9.0` → `dotnet/runtime:9.0`)
- Build image locally and test
- Test the full flow inside the container: MCP tool call → blob download → process → blob upload
- Create the architecture diagram showing MCP client/server and blob container interactions

**Deliverable:** Docker image runs the full pipeline cleanly


---

## Sprint 5 (July 27 – August 9)

**Goal: Deploy to Azure**

- Push Docker image to Azure Container Registry
- Deploy to Azure App Service
- Verify the deployed App Service instance is reachable by an MCP client and connects correctly to blob storage
- Test end-to-end on live Azure: MCP tool call → blob read/write → response returned to client

**Deliverable:** Live Azure deployment running on App Service, reachable by an MCP client, screenshots captured for documentation

---

## Sprint 6 (August 10–23)

**Goal: Polish & Submit**

- Finalize `Experiment Specification - Firstname Lastname.md`
- Review architecture diagram and clean up README to match the actual MCP + blob-only design
- Note the in-memory classifier limitation (trained state is not persisted; a server restart requires retraining) as a documented known limitation / future work item
- Open GitHub issue for review by August 16 — buffer for feedback and fixes
- Address any review comments before August 23
