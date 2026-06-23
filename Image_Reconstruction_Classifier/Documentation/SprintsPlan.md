# Sprint Plan - Image Reconstruction Classifier on Azure

**Deadline: August 23, 2026 | Open review issue by: August 16**

---

## Sprint 1 (June 1–14)

**Goal: Setup & Foundation**

- Clone the template from the repo
- Set up Azure Storage account (container, queue, table)
- Set up environment variables for all connection strings
- Add `SampleFiles` folder with test images committed
- Write skeleton `IExperiment` and `IStorageProvider` in `MyExperiment`

**Deliverable:** Repo structure visible, first commits pushed

---

## Sprint 2 (June 15–28)

**Goal: Bug Fixes + Core Experiment Wired to Cloud Storage**

- Fix ConvertImagesToBinary — use parameters with env var fallback, not the other way around
- Fix the index mismatch between spatial files and imageData array
- Fix hardcoded dev machine fallback paths in ImageSpatial.cs and Program.cs
- Implement `DownloadInputFile` — pull training images from blob storage
- Implement `UploadResultFile` — push result images back to blob
- Implement `UploadExperimentResult` — write `ExperimentResult` record to table storage
- Wire the existing HTM/KNN unit test as the experiment body inside `IExperiment`
- Set up logging concept: define `LogInfo`, `LogError`, `LogDebug`, `Log`

**Deliverable:** Bugs fixed, Experiment runs locally end-to-end — reads from queue, downloads, trains, uploads

---

## Sprint 3 (June 29 – July 12)

**Goal: MCP Tool Integration**

- Refactor the experiment to expose functionality as an MCP Tool
- Support both STDIO and HTTP transport
- Register plugins/tools into the MCP server
- Test MCP agent triggering the experiment locally
- Write `Experiment Specification - Firstname Lastname.md`

**Deliverable:** MCP server runs and triggers the experiment via tool call

---

## Sprint 4 (July 13–26)

**Goal: Dockerize**

- Write `Dockerfile` for `MyExperiment`
- Build image locally and test
- Test the full flow inside the container: queue message → download → train → upload → table update
- Create the architecture diagram showing blob/queue/table/container interactions

**Deliverable:** Docker image runs the full experiment cleanly

---

## Sprint 5 (July 27 – August 9)

**Goal: Deploy to Azure**

- Push Docker image to Azure Container Registry
- Deploy to Azure App Services
- Activate App Service and verify connection to storage
- Test end-to-end on live Azure: queue message → results appear in blob + table

**Deliverable:** Live Azure deployment running, screenshots captured for documentation

---

## Sprint 6 (August 10–23)

**Goal: Polish & Submit**

- Finalize `Experiment Specification - Firstname Lastname.md`
- Review architecture diagram and clean up README
- Open GitHub issue for review by August 16 — buffer for feedback and fixes
- Address any review comments before August 23
