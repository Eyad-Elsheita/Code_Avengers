# PROMPTS.md — AI-Assisted Development Disclosure Log

**Project:** Image Reconstruction Using Hierarchical Temporal Memory and K-Nearest Neighbor Classifiers
**Authors:** Eyad Elsheita, Khaled Kandil

This is the single, merged disclosure log referenced by Section V of the paper. It replaces two earlier separate files — the original development Q&A record (`AI Documenttion.md.txt`) and this session's paper/deliverable-authoring log (the earlier `PROMPTS.md`) — combined here, de-duplicated, and trimmed of thin or repeated entries.

The log is split into two parts because they are genuinely different kinds of AI assistance: **Part 1** is AI used to help design, implement, and debug the actual C#/Azure codebase; **Part 2** is AI used to help write the paper, the presentation, and this project's supporting documents. Only Part 1 is "AI coding assistant" usage in the sense Section III-D of the paper discusses; Part 2 is disclosed for completeness since it's still a significant AI contribution to a graded deliverable.

**On accuracy:** while merging, three claims in the original Part 1 record were checked against the current source code and no longer match it exactly. Rather than silently editing history, each is left in place with a dated verification note attached — the same "disclose the mismatch, don't erase it" approach the paper itself uses for its Table Storage finding.

---

## Part 1 — Code & Cloud Architecture

*Tool used throughout: an AI coding assistant referred to as "Claude" in the original development notes; the specific model/version was not recorded at the time.*

### 1 — Project architecture & planning

- **Prompt:** "We have a local console application that performs image reconstruction using HTM and KNN classifiers. We want to migrate it to the cloud. What should the overall Azure architecture look like?"
- **AI output summary:** Proposed a layered architecture: Azure Blob Storage replacing the local filesystem; Azure App Service hosting the MCP server; Azure Container Registry for the Docker image; Azure Table Storage for reconstruction results; and an MCP tools layer wrapping the existing classifier logic so an AI agent can call each operation independently. Recommended leaving the existing classes (`ImageLoader`, `ImageProcessor`, `ImageSpatial`, `HtmClassifier`, `KnnClassifier`, `ImageSimilarity`, `BinaryToImageConverter`, `ImageFilter`) unchanged and adding a new `Tools/` layer on top.
- **Manual change:** Adopted as the project's target architecture.
- **Reason:** Needed a concrete migration plan before writing any cloud-facing code.

### 2 — MCP tool design

- **Prompt:** "We need to create MCP tools for our existing functionality. What is the correct pattern to follow, and what tools do we need?"
- **AI output summary:** Explained the `[McpServerToolType]` / `[McpServerTool]` / `[Description(...)]` attribute pattern, and proposed 7 tools: `ImageLoaderTool`, `ImageProcessorTool`, `ImageSpatialTool`, `HtmClassifierTool`, `KnnClassifierTool`, `ImageSimilarityTool`, `BinaryToImageConverterTool`, and `ImageFilterTool`.
- **Manual change:** Implemented the 7-tool layer following this pattern.
- **Reason:** Needed a consistent, agent-callable interface over the existing classifier logic.
- **Verification note (checked against current source):** The list above includes a `KnnClassifierTool`, but no such class exists in the current `Tools/` folder — the server registers exactly 7 tool classes (`ImageLoaderTool`, `ImageProcessorTool`, `ImageSpatialTool`, `HtmClassifierTool`, `BinaryToImageConverterTool`, `ImageSimilarityTool`, `ImageFilterTool`), none of which perform KNN classification. `KnnClassifier.cs` still exists but is only called from the offline `LocalPipelineRunner.cs`, not from any MCP tool. This means the live, agent-callable pipeline currently reconstructs via HTM only — KNN and the HTM+KNN fusion described in the paper's Section III-C run in the offline evaluation path, not in the deployed server. Worth deciding whether to wire KNN into a live tool or correct the paper's description of the live flow before submission.

### 3 — Azure Blob Storage integration & container structure

- **Prompt:** "The tools currently read from local filesystem paths. How should we connect them to Azure Blob Storage? Should we use multiple containers (train, test, models, results) or a single container with naming conventions?"
- **AI output summary:** Recommended injecting `BlobServiceClient` via DI into each tool constructor; downloading blobs to a temp folder, processing with the existing classes, uploading results back, and always cleaning up temp files in a `finally` block; and using `GetBlobsAsync(BlobTraits.None, BlobStates.All, prefix, CancellationToken.None)` for prefix-filtered listing. On container structure, walked through the trade-offs (cleaner separation vs. fewer resources) and noted Azure bills for data stored, not for container count, so the decision came down to operational simplicity rather than cost.
- **Manual change:** Settled on exactly two containers, `train` and `test`, with each holding all related file types (original PNGs, binarized `.txt`, spatial SDR `.txt`, reconstructed PNGs, and JSON model files) distinguished by filename suffix rather than by separate containers per artifact type.
- **Reason:** Needed a storage layout simple enough to manage without sacrificing clarity.

### 4 — Debugging: `GetBlobsAsync` signature mismatch

- **Prompt:** "We are getting the error 'There is no argument given that corresponds to the required parameter cancellationToken of BlobContainerClient.GetBlobsAsync'. How do we fix this?"
- **AI output summary:** Identified that the installed Azure SDK version required all four parameters explicitly, and gave the fix: add `CancellationToken.None` as the fourth argument.
- **Manual change:** Applied the fix across all four tool files that called `GetBlobsAsync` — `ImageLoaderTool`, `ImageProcessorTool`, `ImageSpatialTool`, `ImageSimilarityTool`.
- **Reason:** SDK version mismatch was a hard compile error blocking all Blob-dependent tools.

### 5 — HTM classifier persistence

- **Prompt:** "The HTM classifier stores training examples in memory. How do we persist this so it survives Azure App Service restarts and scale-out events?"
- **AI output summary:** Explained that an in-memory singleton loses all training data on restart/scale-out, forcing a full retrain. Proposed adding `ExportTrainingExamples()` / `ImportTrainingExamples()` to `MyHtmClassifier`, a `TrainingExampleDto` to serialize `HashSet<int>` SDRs as `int[]` for clean JSON, and `SaveModelToBlob()` / `LoadModelFromBlob()` on `HtmClassifierTool`.
- **Manual change:** Implemented model persistence to Blob Storage so the agent can train once and reload the model in later sessions without retraining.
- **Reason:** This is the fix that later closed the persistence gap discussed in the paper's Discussion and Scalability sections.

### 6 — Logging system design

- **Prompt:** "We need to create a logging concept for the project with LogInfo, LogError, LogDebug, and Log levels. How should this work in an MCP server?"
- **AI output summary:** Designed `CloudLogger` as a static class with four log-level methods, writing to `Console.Error` (stderr) rather than `Console.WriteLine` (stdout) — because MCP's STDIO transport uses stdout for protocol messages, and logging to stdout would corrupt that stream. Specified a `[timestamp] [LEVEL] [tool name] message` line format.
- **Manual change:** Replaced every `Console.WriteLine` across all 7 tool files with the appropriate `CloudLogger` call.
- **Reason:** Needed observability without risking protocol corruption.

### 7 — Saving results to Azure Table Storage

- **Prompt:** "How should we save reconstruction results (similarity scores, image names, methods used) to a database?"
- **AI output summary:** Recommended Azure Table Storage over SQL — the records are simple and structured, no relational joins needed, and it reuses the existing `imgreconstructionstorage` account rather than provisioning a new resource. Designed the schema: `PartitionKey` = object type (0–9), `RowKey` = `imageName_method_timestamp`. Also caught an OData filter-injection risk in an early draft of the query filter (unsafe string interpolation) and gave the SDK-safe fix using `TableClient.CreateQueryFilter(...)`.
- **Manual change:** Registered `ResultStorageService` as a singleton in `Program.cs`, injected into `ImageSimilarityTool` so results save automatically on every similarity comparison; used the safe filter form throughout.
- **Reason:** Needed durable, queryable storage for evaluation metrics, and needed to close the injection risk before it shipped.

### 8 — Docker and Azure deployment

- **Prompt:** "How do we Dockerize the MCP server and deploy it to Azure App Service?"
- **AI output summary:** Provided a multi-stage `Dockerfile` (`mcr.microsoft.com/dotnet/sdk:9.0` to build, `mcr.microsoft.com/dotnet/runtime:9.0` to run) and the deployment sequence: build the image, test locally, create the ACR, tag and push, create a Linux App Service plan, create the Web App with the container image, set `AZURE_STORAGE_CONNECTION_STRING` and `WEBSITES_PORT=8080` as app settings, restart, verify via Log Stream. Also identified that the original `WithStdioServerTransport()` only works for local processes and needed to be replaced with `WithHttpTransport()` (`ModelContextProtocol.AspNetCore`) plus `app.MapMcp()` for Azure App Service, which communicates over HTTP.
- **Manual change:** Adopted the Dockerfile as-is; switched the server's transport from stdio to HTTP; followed the deployment sequence for the live App Service instance.
- **Reason:** Stdio transport is a non-starter for a platform-hosted HTTP service; the deployment needed a repeatable sequence.

### 9 — Building the agent client

- **Prompt:** "How do we build an AI agent that connects to our MCP server and can call the tools?"
- **AI output summary:** Designed `ImageReconstructionAgent` as a separate console app connecting over HTTP transport (`McpClient.CreateAsync` with `HttpClientTransport`), loading all available tools from the server dynamically, injecting them into the model, and running an interactive loop.
- **Manual change:** Implemented as designed.
- **Reason:** Needed a client that could drive the pipeline through natural language against either a local or Azure-hosted server.
- **Verification note (checked against current source):** The original notes mention the agent calling "any of the 25 registered tools" and, separately, a temporary switch to the Anthropic API when the OpenAI quota was exhausted during testing. Current source registers 7 tool *classes* comprising 32 individual `[McpServerTool]`-attributed methods in total (not 25), and `ImageReconstructionAgent/Program.cs` authenticates against `OPENAI_API_KEY` via `OpenAIClient` — so if the Claude switch happened, it was later reverted, or was limited to a testing session that predates the current `Program.cs`. Neither point changes what was actually built; both are flagged here so the numbers in this log match the numbers in the paper and README.

### 10 — Uploading training images to Azure

- **Prompt:** "How do we upload 10,000 training images and 2,000 test images to Azure Blob Storage efficiently?"
- **AI output summary:** Recommended the Azure CLI `az storage blob upload-batch` command for bulk upload instead of manual Portal uploads, and confirmed the existing filename convention (`0_26.png`, `1_552.png`) already matched the tools' object-type prefix filtering, so no renaming was needed.
- **Manual change:** Uploaded the dataset via `upload-batch` into the `train` and `test` containers.
- **Reason:** Manual, one-by-one Portal uploads were impractical at this file count.

---

## Part 2 — Paper, Presentation & Supporting Documents

*Tool used throughout: Claude, running in Cowork mode (configured model identifier: `claude-sonnet-5`).*

### 11 — Initial paper draft and architecture figure

- **Prompt:** *(reconstructed — see note below)* Write the IMRAD academic paper documenting this project, following the course's IEEE-style formatting guidelines, and embed the team's hand-drawn architecture diagram as Figure 1.
- **AI output summary:** A full first-draft paper (Abstract through Conclusion, References, Appendix) structured around the actual codebase, with the diagram inserted as a full-width figure captioned to match the architecture as it existed at the time.
- **Manual change:** Author team reviewed the draft against the real system and flagged claims needing verification before submission.
- **Reason:** Establish a complete, structurally correct starting point before refining formatting and content accuracy.

### 12 — IEEE formatting corrections

- **Prompt:** *(reconstructed)* Review the draft against actual IEEE conference formatting rules and fix any deviations.
- **AI output summary:** Corrected heading levels, column layout, caption placement, and citation style to match IEEE conventions.
- **Manual change:** Author team confirmed the corrected formatting matched the course's expected style.
- **Reason:** The original draft approximated IEEE style rather than following it precisely.

### 13 — Genuine IEEEtran LaTeX version

- **Prompt:** *(reconstructed)* Produce an actual `IEEEtran`-class LaTeX version of the paper, not just a Word document styled to look like one.
- **AI output summary:** A `main.tex` file using `\documentclass[conference]{IEEEtran}`, compiled with `pdflatex` into a verified PDF.
- **Manual change:** Requested specifically because a Word approximation was judged insufficient for a course expecting real LaTeX submissions.
- **Reason:** Match the submission format conventions of the target venue/course exactly, not approximately.

### 14 — Content-reuse measurement

- **Prompt:** *(reconstructed)* Measure what percentage of this paper's content was reused from an earlier ML-course document on the same underlying algorithm.
- **AI output summary:** A comparison identifying which passages (largely the HTM/KNN algorithm description and accuracy results) originated in the earlier document versus new material specific to this course's cloud/engineering scope.
- **Manual change:** Used to confirm the paper's original contribution was substantial enough, independent of the reused ML background.
- **Reason:** Academic-integrity self-check before submission.

### 15 — Make the agent-via-Azure approach and scalability explicit

- **Prompt:** *(reconstructed)* Make sure the paper explicitly states that the AI agent accesses the reconstruction tools via the Azure-hosted server, and add a dedicated scalability discussion.
- **AI output summary:** New "Scalability Considerations" subsection, plus edits naming the agent-to-Azure-hosted-server relationship as the project's main architectural approach.
- **Manual change:** Author team confirmed this was the correct characterization of the delivered product's architecture.
- **Reason:** This relationship is central to the project and was previously only implicit.

### 16 — Remove placeholders, reach 9 pages honestly

- **Prompt:** *(reconstructed)* Remove every `[PLACEHOLDER]` marker — including a fabricated-looking AI Usage Log table — and grow the paper to exactly 9 pages using only real, already-available evidence.
- **AI output summary:** Replaced placeholders with measured facts (LOC counts via `wc -l`, tool method signatures verified against the `.cs` source, dependency versions from the `.csproj` files); reached 9 pages through legitimate elaboration, not padding.
- **Manual change:** Accepted the expanded content after confirming every added fact was independently verifiable in the repository.
- **Reason:** Placeholder and fabricated-looking content is a direct academic-integrity risk in a paper that specifically claims to disclose AI usage honestly.

### 17 — Swap diagrams, trim to 9 pages, drop the Appendix, add letterhead

- **Prompt (verbatim):** "some changes are needed, replace the Architecture image with the 'main architecture' image and add the 'inner Tool Connection'. tell me your opinion as well. also make the doc 9 pages with the reference part and remove the APPENDIX part, I saved them in another separate file, If you believe you wanna add something else also add it in another separate .md file only if needed. last change needed make sure to include the logo of the Uni at the top corner and make sure the headline like the pic I attached. got it?" *(with 5 reference images attached)*
- **AI output summary:** Flagged a factual contradiction before editing (new diagrams showed Table Storage active and App Service as host, conflicting with previously-confirmed facts), resolved it via three clarifying questions, then replaced Figure 1, added the tool-flow diagram as Figure 2, renumbered all figures/tables/references, rewrote the storage-architecture narrative as resolved, removed the Appendix, and added the university logo and real author/title block to the Word document only.
- **Manual change:** Confirmed the updated architectural facts reflect the real current state of the project; chose to keep both file formats in sync while keeping the LaTeX/PDF strictly IEEE-compliant.
- **Reason:** Keep the paper's most safety-critical property — never asserting an unconfirmed fact — intact even as the underlying system changed.

### 18 — Five-minute, two-presenter deck with a separate script

- **Prompt (verbatim):** "cool, create me a presentation mostly for the new approach applied here for 5 minutes talk between 2 presenters, provide the script in a separate file please"
- **AI output summary:** An 11-slide deck built with real project diagrams and data, plus a standalone speaker script timed to fit both presenters within ~5 minutes.
- **Manual change:** Specified the deck should cover the whole project with the new architecture emphasized, and that the 5 minutes should split 2:30/2:30 with the second presenter closing; script was trimmed after an initial draft ran long.
- **Reason:** A course presentation needs a scoped, time-boxed script two people can actually deliver in the allotted slot.

### 19 — README.md for the cloud project

- **Prompt (verbatim):** "so let's now do the README.md file for the cloud project, please make sure to include. don't copy old stuff from the Software Eningeering Project\n\nenvironmential variables \nhow to run the project with the following:\n-Main proj\n-Run Agents"
- **AI output summary:** Read the actual source (`Image_Reconstruction_Classifier` and `ImageReconstructionAgent`) rather than reusing course boilerplate, and wrote a README covering both projects' real environment variables (`AZURE_STORAGE_CONNECTION_STRING`, `OPENAI_API_KEY`, `OPENAI_CHAT_MODEL_NAME`, `MCP_SERVER_URL`), how to run each with `dotnet run` or Docker, and the current Azure deployment details.
- **Manual change:** None yet requested.
- **Reason:** A grader needs to be able to actually run the project from the README, not from a generic template.

### 20 — This merged disclosure log

- **Prompt (verbatim):** "merge all of them in one .md file name it PROMPTS and make sure to delete the silly ones or repeated prompts" *(with the original `AI Documenttion.md.txt` and the prior `PROMPTS.md` attached)*
- **AI output summary:** This file — merged both sources, cut one thin entry (a standalone "diagram embedded" entry folded into entry 11) and one duplicate entry (a second container-structure discussion folded into entry 3), and added three verification notes where the original development record no longer matches the current source code.
- **Manual change:** Author to confirm the three verification notes (entries 2 and 9) are read and either acted on or consciously left as-is before submission.
- **Reason:** The paper promises this log as the verifiable source of truth behind its AI-usage disclosure — it needed to be one accurate file, not two overlapping ones.

---

