import { useRef, useState } from "react";
import { AiExplanation, DetectorCard, StatusBadge } from "./components/AnalysisCards";
import type { DetectorResult } from "./components/AnalysisCards";
import "./App.css";
type UploadResult = {
  mediaId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  objectKey: string;
  isDuplicate: boolean;
};
type AnalysisJob = {
  jobId: string;
  mediaId: string;
  objectKey: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};
type AnalysisStatus = "idle" | "uploading" | "analyzing" | "completed" | "failed";
function App() {
  const [file, setFile] = useState<File | null>(null);
  const [dragging, setDragging] = useState(false);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [uploadResult, setUploadResult] = useState<UploadResult | null>(null);
  const [detectorResults, setDetectorResults] = useState<DetectorResult[]>([]);
  const [analysisJob, setAnalysisJob] = useState<AnalysisJob | null>(null);
  const [analysisStatus, setAnalysisStatus] = useState<AnalysisStatus>("idle");
  const [showHistory, setShowHistory] = useState(false);
  const [jobs, setJobs] = useState<AnalysisJob[]>([]);
  const [showJobs, setShowJobs] = useState(false);
  const [jobsLoading, setJobsLoading] = useState(false);
  const [jobsError, setJobsError] = useState("");
  const [jobSearch, setJobSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("All");
  const [selectedJob, setSelectedJob] = useState<AnalysisJob | null>(null);
  const [selectedJobResults, setSelectedJobResults] = useState<DetectorResult[]>([]);
  const [selectedJobResultsLoading, setSelectedJobResultsLoading] = useState(false);
  const [selectedJobResultsError, setSelectedJobResultsError] = useState("");
  const selectedJobRequestId = useRef(0);
  const busy = loading || analysisStatus === "analyzing";
  const pollForAnalysis = async (mediaId: string) => {
    setAnalysisStatus("analyzing");
    for (let attempt = 1; attempt <= 30; attempt++) {
      const response = await fetch(`http://localhost:8082/jobs/media/${mediaId}`);
      if (!response.ok) throw new Error("We couldn’t check the analysis status. Please try again.");
      const job: AnalysisJob = await response.json();
      setAnalysisJob(job);
      if (job.status === "Failed") {
        setAnalysisStatus("failed");
        setMessage("Analysis failed. Please try again.");
        return;
      }
      if (job.status === "Completed") {
        const resultsResponse = await fetch(`http://localhost:8083/detectors/results/${mediaId}`);
        if (!resultsResponse.ok) throw new Error("We couldn’t load the detector results. Please try again.");
        const results: DetectorResult[] = await resultsResponse.json();
        setDetectorResults(results);
        setAnalysisStatus("completed");
        return;
      }
      await new Promise((resolve) => setTimeout(resolve, 1000));
    }
    setAnalysisStatus("failed");
    setMessage("Analysis is taking longer than expected. Check Job History for its latest status.");
  };
  const resetAnalysis = () => {
    setMessage("");
    setDetectorResults([]);
    setAnalysisJob(null);
    setShowHistory(false);
  };
  const handleUpload = async () => {
    if (!file || busy) return;
    try {
      setLoading(true);
      resetAnalysis();
      setUploadResult(null);
      setAnalysisStatus("uploading");
      const formData = new FormData();
      formData.append("file", file);
      const response = await fetch("http://127.0.0.1:8081/media", { method: "POST", body: formData });
      if (!response.ok) throw new Error("We couldn’t upload this file. Please check the media service and try again.");
      const data: UploadResult = await response.json();
      setUploadResult(data);
      setLoading(false);
      if (data.isDuplicate) {
        setAnalysisStatus("idle");
        return;
      }
      await pollForAnalysis(data.mediaId);
    } catch (error) {
      console.error("Media upload error:", error);
      setAnalysisStatus("failed");
      setMessage(error instanceof TypeError ? "We couldn’t reach the analysis services. Check that the backend is running and try again." : error instanceof Error ? error.message : "Something went wrong. Please try again.");
    } finally {
      setLoading(false);
    }
  };
  const handleAnalyzeAgain = async () => {
    if (!uploadResult || busy) return;
    try {
      resetAnalysis();
      setAnalysisStatus("analyzing");
      const response = await fetch("http://localhost:8082/jobs", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ mediaId: uploadResult.mediaId, objectKey: uploadResult.objectKey, contentType: uploadResult.contentType, sizeBytes: uploadResult.sizeBytes }),
      });
      if (!response.ok) throw new Error("We couldn’t start a new analysis. Please try again.");
      await pollForAnalysis(uploadResult.mediaId);
    } catch (error) {
      console.error("Analyze again error:", error);
      setAnalysisStatus("failed");
      setMessage(error instanceof TypeError ? "We couldn’t reach the analysis services. Check that the backend is running and try again." : error instanceof Error ? error.message : "We couldn’t start a new analysis. Please try again.");
    }
  };
  const handleLoadJobs = async () => {
    setJobsLoading(true);
    setJobsError("");
    try {
      const response = await fetch("http://localhost:8082/jobs");
      if (!response.ok) throw new Error("Job history request failed");
      const data: AnalysisJob[] = await response.json();
      setJobs(data);
      setShowJobs(true);
    } catch (error) {
      console.error("Job history error:", error);
      setJobsError("We couldn’t load Job History. Check that the backend is running and try again.");
    } finally {
      setJobsLoading(false);
    }
  };
  const handleSelectJob = async (job: AnalysisJob) => {
    const requestId = ++selectedJobRequestId.current;
    setSelectedJob(job);
    setSelectedJobResults([]);
    setSelectedJobResultsLoading(true);
    setSelectedJobResultsError("");
    try {
      const response = await fetch(`http://localhost:8083/detectors/results/job/${job.jobId}`);
      if (response.status === 404) return;
      if (!response.ok) throw new Error("Job analysis request failed");
      const results: DetectorResult[] = await response.json();
      // A response from an earlier selection must never replace the current job's results.
      if (requestId === selectedJobRequestId.current) setSelectedJobResults(results);
    } catch (error) {
      console.error("Selected job results error:", error);
      if (requestId === selectedJobRequestId.current) setSelectedJobResultsError("We couldn’t load this job’s analysis result. Please try again.");
    } finally {
      if (requestId === selectedJobRequestId.current) setSelectedJobResultsLoading(false);
    }
  };
  const selectFile = (selectedFile: File | undefined) => {
    if (!selectedFile || busy) return;
    if (selectedFile.size > 500 * 1024 * 1024) {
      setMessage("File is too large. Maximum size is 500 MB.");
      setFile(null);
      return;
    }
    if (selectedFile.type && !selectedFile.type.startsWith("image/") && !selectedFile.type.startsWith("video/")) {
      setMessage("Please select an image or video file.");
      return;
    }
    setFile(selectedFile);
    resetAnalysis();
    setUploadResult(null);
    setAnalysisStatus("idle");
  };
  const sortedResults = [...detectorResults].sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime());
  const latestResult = sortedResults[0];
  const previousResults = sortedResults.slice(1);
  const filteredJobs = jobs.filter((job) => {
    const search = jobSearch.toLowerCase();
    return ((job.objectKey.split("/").pop()?.toLowerCase() ?? "").includes(search) || job.mediaId.toLowerCase().includes(search)) && (statusFilter === "All" || job.status === statusFilter);
  });
  const latestExplanationJobId = latestResult?.jobId ?? analysisJob?.jobId;
  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="#main"><span className="brand-mark" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M12 3 20 6v6c0 4-4 7-8 9-4-2-8-5-8-9V6l8-3Z" stroke="currentColor" strokeWidth="1.7" />
            <path d="m8 12 3 3 5-6" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </span>MediaTrust<span className="brand-divider">/</span><span className="brand-caption">Analysis workspace</span></a>
        <span className="workspace-label">Media integrity platform</span>
      </header>
      <main id="main" className="dashboard">
        <div className="page-heading">
          <div>
            <span className="eyebrow">Media intelligence</span>
            <h1>Understand what’s behind the media.</h1>
            <p>Secure Media Analysis &amp; Integrity Verification</p>
          </div>
          <a className="text-link" href="#job-history">Explore job history <span aria-hidden="true">↗</span></a>
        </div>
        <div className="workspace-grid">
          <section className="panel upload-panel" aria-labelledby="upload-title">
            <div className="section-heading">
              <div>
                <span className="eyebrow">01 / Submit media</span>
                <h2 id="upload-title">Start an analysis</h2>
              </div>
              <span className="small-tag">Image &amp; video</span>
            </div>
            <p className="muted">Upload your media to inspect it for potential manipulation.</p>
            <label className={`upload-box ${dragging ? "is-dragging" : ""} ${busy ? "is-disabled" : ""}`} onDragOver={(event) => {
              event.preventDefault();
              if (!busy) setDragging(true);
            }} onDragLeave={() => setDragging(false)} onDrop={(event) => {
              event.preventDefault();
              setDragging(false);
              selectFile(event.dataTransfer.files[0]);
            }}>
              <input type="file" accept="image/*,video/*" disabled={busy} onChange={(event) => selectFile(event.target.files?.[0])} aria-label="Choose an image or video to analyze" />
              <span className="upload-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none">
                  <path d="M12 16V4m-4 4 4-4 4 4M4 15v5h16v-5" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </span>
              <strong>
                {file ? "Your media is ready" : "Drop your media here"}
              </strong>
              <span>or <span className="browse-link">browse files</span> to upload</span>
              <small>Images and videos · Up to 500 MB</small>
            </label>
            <div className={`file-info ${file ? "has-file" : ""}`}>
              <span className="file-symbol" aria-hidden="true">▤</span>
              <div>
                <strong>
                  {file ? file.name : "No file selected"}
                </strong>
                <span>
                  {file ? `${(file.size / 1024 / 1024).toFixed(2)} MB · Ready to analyze` : "Choose an image or video to get started."}
                </span>
              </div>
            </div>
            <button className="button primary analyze-button" disabled={!file || busy} onClick={handleUpload}>
              {busy && <span className="spinner" />}
              {analysisStatus === "uploading" ? "Uploading media…" : analysisStatus === "analyzing" ? "Analyzing media…" : "Analyze Media"}
              {!busy && <span aria-hidden="true">→</span>}
            </button>
            <p className="upload-note">Each analysis creates a traceable job with its own results.</p>
            {message && <div className="error-message" role="alert">
              <strong>Something needs your attention</strong>
              <p>
                {message}
              </p>
            </div>}
            {uploadResult && <section className="upload-summary">
              <div className="section-heading">
                <h3>
                  {uploadResult.isDuplicate ? "Media already in your library" : "Media uploaded"}
                </h3>
                <StatusBadge status={analysisJob?.status ?? (analysisStatus === "failed" ? "Failed" : analysisStatus === "idle" ? "Uploaded" : "Pending")} />
              </div>
              <dl className="metadata">
                <div>
                  <dt>File name</dt>
                  <dd>
                    {uploadResult.fileName}
                  </dd>
                </div>
                <div>
                  <dt>Media ID</dt>
                  <dd className="mono">
                    {uploadResult.mediaId}
                  </dd>
                </div>
                <div>
                  <dt>Type</dt>
                  <dd>
                    {uploadResult.contentType}
                  </dd>
                </div>
                <div>
                  <dt>Size</dt>
                  <dd>{(uploadResult.sizeBytes / 1024).toFixed(2)} KB</dd>
                </div>
              </dl>
              <div className="reanalyze">
                <button className="button secondary" onClick={handleAnalyzeAgain} disabled={busy}>↻ Analyze Again</button>
                <p>Create a new analysis job for the same media.</p>
              </div>
            </section>}
          </section>
          <section className="panel latest-panel" aria-labelledby="latest-title" aria-busy={busy}>
            <div className="section-heading">
              <div>
                <span className="eyebrow">02 / Review findings</span>
                <h2 id="latest-title">Latest Analysis</h2>
              </div>
              <span className="small-tag">Detector results</span>
            </div>
            {busy ? <div className="empty-state" role="status">
              <span className="state-icon">
                <span className="spinner" />
              </span>
              <h3>
                {analysisStatus === "uploading" ? "Uploading your media" : "Analysis in progress"}
              </h3>
              <p>
                {analysisStatus === "uploading" ? "Preparing your file for analysis." : "The detectors are examining your media. Results will appear here."}
              </p>
              <div className="progress-track">
                <span />
              </div>
            </div> : analysisStatus === "completed" && latestResult ? <><DetectorCard result={latestResult} /><p className="result-note">Detector scores indicate risk signals, not a final authenticity verdict.</p>{latestExplanationJobId && <AiExplanation key={latestResult.id} jobId={latestExplanationJobId} />}</> : <div className="empty-state">
              <span className="state-icon" aria-hidden="true">◎</span>
              <h3>
                {analysisStatus === "failed" ? "Analysis couldn’t be completed" : analysisStatus === "completed" ? "No detector results available" : "Your next insight starts here"}
              </h3>
              <p>
                {analysisStatus === "failed" ? "Review the message in the upload panel and try again." : analysisStatus === "completed" ? "The job finished without detector results. You can create a new analysis for this media." : uploadResult?.isDuplicate ? "This media was uploaded before. Choose Analyze Again to create a new job, or explore its jobs in Job History." : "Submit an image or video to see detector findings, risk scores, and an AI explanation."}
              </p>
              <div className="empty-features">
                <span>Detector findings</span>
                <span>Risk assessment</span>
                <span>AI insights</span>
              </div>
            </div>}
          </section>
        </div>
        {analysisStatus === "completed" && previousResults.length > 0 && <section className="panel analysis-history">
          <div className="section-heading">
            <div>
              <span className="eyebrow">Previous detector results</span>
              <h2>Analysis History <span className="count">
                {previousResults.length}
              </span></h2>
              <p className="muted">Earlier findings for the current media, separate from the latest analysis.</p>
            </div>
            <button className="button secondary" aria-expanded={showHistory} onClick={() => setShowHistory(!showHistory)}>
              {showHistory ? "Hide history" : "View analysis history"}
            </button>
          </div>
          {showHistory && <div className="history-grid">
            {previousResults.map((result) => <DetectorCard key={result.id} result={result} />)}
          </div>}
        </section>}
        <section id="job-history" className="panel job-history" aria-labelledby="jobs-title" aria-busy={jobsLoading}>
          <div className="section-heading">
            <div>
              <span className="eyebrow">Analysis audit trail</span>
              <h2 id="jobs-title">Job History</h2>
              <p className="muted">Every analysis, with the findings that belong to that exact job.</p>
            </div>
            <div className="history-actions">
              {showJobs && <button className="button text-button" onClick={handleLoadJobs} disabled={jobsLoading}>↻ Refresh</button>}
              <button className="button secondary" aria-expanded={showJobs} disabled={jobsLoading} onClick={() => showJobs ? setShowJobs(false) : handleLoadJobs()}>
                {jobsLoading ? <><span className="spinner" />Loading jobs…</> : showJobs ? "Hide Job History" : "View Job History"}
              </button>
            </div>
          </div>
          {jobsError && <p className="error-message" role="alert">
            {jobsError}
          </p>}
          {!showJobs && !jobsLoading && <div className="history-placeholder">
            <span aria-hidden="true">▤</span>
            <p>Open Job History to browse previous jobs and inspect their individual results.</p>
          </div>}
          {jobsLoading && <p className="loading-state" role="status"><span className="spinner" />Loading job history…</p>}
          {showJobs && <><div className="job-filters">
            <label className="search-control">
              <span>Search jobs</span>
              <input type="search" placeholder="Search file name or Media ID…" value={jobSearch} onChange={(event) => setJobSearch(event.target.value)} />
            </label>
            <label className="status-control">
              <span>Status</span>
              <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
                {["All", "Pending", "Processing", "Completed", "Failed"].map((status) => <option key={status} value={status}>
                  {status === "All" ? "All Statuses" : status}
                </option>)}
              </select>
            </label>
            <p className="jobs-count">Showing <strong>
              {filteredJobs.length}
            </strong> of {jobs.length} jobs</p>
          </div>
            <div className={`jobs-layout ${selectedJob ? "has-selection" : ""}`}>
              <div className="jobs-table-wrapper">
                <table className="jobs-table">
                  <caption className="sr-only">Analysis jobs. Select a file name to view its job details and results.</caption>
                  <thead>
                    <tr>
                      <th scope="col">File Name</th>
                      <th scope="col">Status</th>
                      <th scope="col">Created</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredJobs.map((job) => <tr key={job.jobId} className={`job-row ${selectedJob?.jobId === job.jobId ? "is-selected" : ""}`} onClick={() => handleSelectJob(job)}>
                      <td>
                        <button className="job-select" aria-pressed={selectedJob?.jobId === job.jobId} onClick={(event) => {
                          event.stopPropagation();
                          handleSelectJob(job);
                        }}>
                          <span className="table-file-icon" aria-hidden="true">▤</span>
                          <span className="table-file-name" title={job.objectKey.split("/").pop()}>
                            {job.objectKey.split("/").pop()}
                          </span>
                          <span className="row-arrow" aria-hidden="true">↗</span>
                        </button>
                      </td>
                      <td>
                        <StatusBadge status={job.status} />
                      </td>
                      <td className="table-date">
                        {new Date(job.createdAtUtc).toLocaleString()}
                      </td>
                    </tr>)}
                  </tbody>
                </table>
                {filteredJobs.length === 0 && <div className="empty-state compact">
                  <h3>
                    {jobs.length === 0 ? "No analysis jobs yet" : "No matching jobs"}
                  </h3>
                  <p>
                    {jobs.length === 0 ? "Upload media to create your first analysis job." : "Try a different file name, Media ID, or status filter."}
                  </p>
                </div>}
              </div>
              {selectedJob && <aside className="job-details" aria-labelledby="job-details-title">
                <div className="section-heading">
                  <div>
                    <span className="eyebrow">Selected analysis</span>
                    <h2 id="job-details-title">Job Details</h2>
                  </div>
                  <StatusBadge status={selectedJob.status} />
                </div>
                <dl className="metadata">
                  <div>
                    <dt>File name</dt>
                    <dd>
                      {selectedJob.objectKey.split("/").pop()}
                    </dd>
                  </div>
                  <div>
                    <dt>Job ID</dt>
                    <dd className="mono">
                      {selectedJob.jobId}
                    </dd>
                  </div>
                  <div>
                    <dt>Media ID</dt>
                    <dd className="mono">
                      {selectedJob.mediaId}
                    </dd>
                  </div>
                  <div>
                    <dt>Status</dt>
                    <dd>
                      {selectedJob.status}
                    </dd>
                  </div>
                  <div>
                    <dt>Created</dt>
                    <dd>
                      {new Date(selectedJob.createdAtUtc).toLocaleString()}
                    </dd>
                  </div>
                  <div>
                    <dt>Updated</dt>
                    <dd>
                      {selectedJob.updatedAtUtc ? new Date(selectedJob.updatedAtUtc).toLocaleString() : "—"}
                    </dd>
                  </div>
                </dl>
                <section className="selected-job-analysis" aria-busy={selectedJobResultsLoading}>
                  <h3>Analysis Result</h3>
                  <p className="muted">Findings for this job only.</p>
                  <div aria-live="polite">
                    {selectedJobResultsLoading ? <p className="loading-state"><span className="spinner" />Loading analysis result…</p> : selectedJobResultsError ? <div className="error-message" role="alert">
                      <p>
                        {selectedJobResultsError}
                      </p>
                      <button className="button secondary" onClick={() => handleSelectJob(selectedJob)}>Try again</button>
                    </div> : selectedJobResults.length === 0 ? <p className="job-empty">No job-specific analysis result is available for this analysis.</p> : <>{selectedJobResults.map((result) => <DetectorCard key={result.id} result={result} />)}<AiExplanation key={selectedJob.jobId} jobId={selectedJob.jobId} /></>}
                  </div>
                </section>
              </aside>}
            </div></>}
        </section>
        <footer className="footer">
          <span>MediaTrust <span className="footer-dot">·</span> Media integrity, made clearer.</span>
          <span>Inspect signals. Make informed decisions.</span>
        </footer>
      </main>
    </div>
  );
}
export default App;
